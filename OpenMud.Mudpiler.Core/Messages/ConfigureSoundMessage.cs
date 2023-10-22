using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Core.Messages
{
    public enum SoundConfiguration
    {
        Stop,
        Play,
        Loop
    }

    public struct ConfigureSoundMessage
    {
        public readonly string? EntityIdentifierScope;
        public readonly string? Sound;
        public readonly int Channel;
        public readonly SoundConfiguration Configuration;

        public ConfigureSoundMessage(string? entityIdentifierScope, string? sound, int channel, SoundConfiguration configuration)
        {
            EntityIdentifierScope = entityIdentifierScope;
            Sound = sound;
            Channel = channel;
            Configuration = configuration;
        }
    }
}
