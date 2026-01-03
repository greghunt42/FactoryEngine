using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class InputMovementSystem : SystemBase
{
    public InputMovementSystem()
    {
        DeclareAccess(builder => builder
            .Reads<Transform2D>()
            .Reads<PhysicsBody>()
            .Writes<Velocity2D>()
            .Writes<AirDodge>());
    }

    protected override void OnRun(SystemContext context)
    {
        var input = context.Services.Input;
        var right = input.GetActionState("move_right").IsPressed ? 1f : 0f;
        var left = input.GetActionState("move_left").IsPressed ? 1f : 0f;
        var jumpAction = input.GetActionState("jump");
        var jumpPressed = jumpAction.IsPressed;
        var justPressed = jumpPressed && !_jumpHeld;
        var justReleased = !jumpPressed && _jumpHeld;
        _jumpHeld = jumpPressed;
        var dashAction = input.GetActionState("air_dodge");
        var dashPressed = dashAction.IsPressed && !_airDodgeHeld;
        _airDodgeHeld = dashAction.IsPressed;
        var delta = right - left;
        foreach (var entity in World!.Query(builder => builder
                     .All<Transform2D>()
                     .All<Velocity2D>()
                     .All<PhysicsBody>()))
        {
            ref var transform = ref World.GetComponent<Transform2D>(entity);
            ref var velocity = ref World.GetComponent<Velocity2D>(entity);
            ref var body = ref World.GetComponent<PhysicsBody>(entity);
            var baseSpeed = body.RunSpeed > 0f ? body.RunSpeed : 4f;
            var airborneMultiplier = Math.Max(0.1f, body.AirControlMultiplier);
            var inputStrength = Math.Abs(delta);
            if (!body.Grounded && body.AirControlExponent > 0f)
            {
                inputStrength = MathF.Pow(inputStrength, body.AirControlExponent);
            }
            var adjustedDelta = delta < 0f ? -inputStrength : inputStrength;
            var target = adjustedDelta * (body.Grounded ? baseSpeed : baseSpeed * airborneMultiplier);
            var accel = body.Grounded ? body.GroundAcceleration : body.AirAcceleration;
            if (accel > 0f)
            {
                var maxDelta = accel * context.DeltaTime;
                velocity.VX = MoveTowards(velocity.VX, target, maxDelta);
            }
            else
            {
                velocity.VX = target;
            }
            if (body.WallJumpCooldownRemaining > 0f)
            {
                body.WallJumpCooldownRemaining = Math.Max(0f, body.WallJumpCooldownRemaining - context.DeltaTime);
            }

            var canJump = body.JumpSpeed > 0f && (body.Grounded || body.RemainingCoyoteTime > 0f || body.JumpQueued);
            if (justPressed && canJump)
            {
                velocity.VY = -body.JumpSpeed;
                body.Grounded = false;
                body.RemainingCoyoteTime = 0f;
                body.JumpQueued = false;
            }
            else if (justPressed && !canJump && body.IsWallSliding && body.WallJumpHorizontalSpeed > 0f && body.WallJumpCooldownRemaining <= 0f)
            {
                var direction = body.WallSlideSide == 0 ? -Math.Sign(delta) : -body.WallSlideSide;
                if (direction == 0)
                {
                    direction = -1;
                }
                velocity.VX = direction * body.WallJumpHorizontalSpeed;
                velocity.VY = -body.JumpSpeed;
                body.IsWallSliding = false;
                body.WallSlideTimer = 0f;
                body.WallJumpCooldownRemaining = body.WallJumpCooldown;
            }
            else if (!body.Grounded && jumpAction.IsPressed && !body.JumpQueued)
            {
                body.JumpQueued = true;
            }

            if (justReleased && velocity.VY < 0f && body.JumpCutMultiplier > 0f)
            {
                var cut = Math.Clamp(body.JumpCutMultiplier, 0f, 1f);
                velocity.VY *= cut;
            }

            var wallSliding = false;
            var canAttemptSlide = !body.Grounded &&
                                  body.WallSlideSpeed > 0f &&
                                  velocity.VY > body.WallSlideSpeed;
            var againstLeft = canAttemptSlide && transform.X <= body.MinX + 0.5f && delta < 0f;
            var againstRight = canAttemptSlide && transform.X >= body.MaxX - 0.5f && delta > 0f;
            if (body.IsWallSliding)
            {
                wallSliding = canAttemptSlide && (againstLeft || againstRight);
            }
            else if (canAttemptSlide && (againstLeft || againstRight) && body.WallSlideTimer <= 0f)
            {
                wallSliding = true;
            }

            if (wallSliding)
            {
                body.WallSlideSide = againstLeft ? -1 : (againstRight ? 1 : body.WallSlideSide);
                velocity.VY = Math.Min(velocity.VY, body.WallSlideSpeed);
                body.WallSlideTimer = body.WallSlideStickTime;
            }
            else
            {
                body.WallSlideSide = 0;
                if (body.WallSlideTimer > 0f)
                {
                    body.WallSlideTimer = Math.Max(0f, body.WallSlideTimer - context.DeltaTime);
                }
            }

            body.IsWallSliding = wallSliding;

            if (World.HasComponent<AirDodge>(entity))
            {
                ref var dodge = ref World.GetComponent<AirDodge>(entity);
                var dt = context.DeltaTime;
                if (body.Grounded)
                {
                    dodge.CooldownRemaining = 0f;
                }
                else if (dodge.CooldownRemaining > 0f)
                {
                    dodge.CooldownRemaining = Math.Max(0f, dodge.CooldownRemaining - dt);
                }

                if (dodge.EffectTimer > 0f)
                {
                    dodge.EffectTimer = Math.Max(0f, dodge.EffectTimer - dt);
                }

                if (dashPressed && dodge.Enabled && !body.Grounded && dodge.CooldownRemaining <= 0f)
                {
                    var direction = Math.Sign(delta);
                    if (direction == 0)
                    {
                        direction = velocity.VX >= 0f ? 1 : -1;
                    }

                    if (direction == 0)
                    {
                        direction = 1;
                    }

                    velocity.VX = direction * dodge.Speed;
                    dodge.CooldownRemaining = dodge.Cooldown;
                    dodge.EffectTimer = Math.Max(dodge.EffectDuration, 0f);
                    dodge.LastDirection = direction;
                }
            }
        }
    }

    private bool _jumpHeld;
    private bool _airDodgeHeld;

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + Math.Sign(target - current) * maxDelta;
    }
}
