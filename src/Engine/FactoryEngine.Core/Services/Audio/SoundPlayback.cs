using System;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Audio;

public readonly record struct SoundPlayback(Guid Id, string SoundKey, AssetId Asset, AudioParams Parameters, DateTime Timestamp);
