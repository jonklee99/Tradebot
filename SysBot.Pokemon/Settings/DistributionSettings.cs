using PKHeX.Core;
using SysBot.Base;
using System;
using System.ComponentModel;
using System.Linq;

namespace SysBot.Pokemon;

public class DistributionSettings : ISynchronizationSetting, ICustomTypeDescriptor
{
    private const string Distribute = nameof(Distribute);

    private const string Synchronize = nameof(Synchronize);

    [Browsable(false)]
    public ProgramMode CurrentMode { get; set; } = ProgramMode.None;

    [Category(Distribute), Description("When enabled, idle LinkTrade bots will randomly distribute PKM files from the DistributeFolder.")]
    public bool DistributeWhileIdle { get; set; } = true;

    [Category(Distribute), Description("When set to true, Random Ledy nickname-swap trades will quit rather than trade a random entity from the pool.")]
    public bool LedyQuitIfNoMatch { get; set; }

    [Category(Distribute), Description("When set to something other than None, the Random Trades will require this species in addition to the nickname match.")]
    public Species LedySpecies { get; set; } = Species.None;

    [Category(Distribute), Description("Distribution Trade Link Code uses the Min and Max range rather than the fixed trade code.")]
    public bool RandomCode { get; set; }

    [Category(Distribute), Description("For LGPE, the first Picto code to use for distribution trades.")]
    public Pictocodes LGPECode1 { get; set; } = Pictocodes.Pikachu;

    [Category(Distribute), Description("For LGPE, the second Picto code to use for distribution trades.")]
    public Pictocodes LGPECode2 { get; set; } = Pictocodes.Pikachu;

    [Category(Distribute), Description("For LGPE, the third Picto code to use for distribution trades.")]
    public Pictocodes LGPECode3 { get; set; } = Pictocodes.Pikachu;

    [Category(Distribute), Description("For BDSP, the distribution bot will go to a specific room and remain there until the bot is stopped.")]
    public bool RemainInUnionRoomBDSP { get; set; } = true;

    // Distribute
    [Category(Distribute), Description("When enabled, the DistributionFolder will yield randomly rather than in the same sequence.")]
    public bool Shuffled { get; set; }

    [Category(Synchronize), Description("Link Trade: Using multiple distribution bots -- all bots will confirm their trade code at the same time. When Local, the bots will continue when all are at the barrier. When Remote, something else must signal the bots to continue.")]
    public BotSyncOption SynchronizeBots { get; set; } = BotSyncOption.LocalSync;

    // Synchronize
    [Category(Synchronize), Description("Link Trade: Using multiple distribution bots -- once all bots are ready to confirm trade code, the Hub will wait X milliseconds before releasing all bots.")]
    public int SynchronizeDelayBarrier { get; set; }

    [Category(Synchronize), Description("Link Trade: Using multiple distribution bots -- how long (seconds) a bot will wait for synchronization before continuing anyways.")]
    public double SynchronizeTimeout { get; set; } = 90;

    [Category(Distribute), Description("Distribution Trade Link Code.")]
    public int TradeCode { get; set; } = 7196;

    public override string ToString() => "Distribution Trade Settings";

    // Visibility control methods for JSON serialization
    public bool ShouldSerializeTradeCode() => CurrentMode != ProgramMode.LGPE;
    public bool ShouldSerializeLGPECode1() => CurrentMode == ProgramMode.LGPE;
    public bool ShouldSerializeLGPECode2() => CurrentMode == ProgramMode.LGPE;
    public bool ShouldSerializeLGPECode3() => CurrentMode == ProgramMode.LGPE;

    // ICustomTypeDescriptor implementation for PropertyGrid visibility
    public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);
    public string? GetClassName() => TypeDescriptor.GetClassName(this, true);
    public string? GetComponentName() => TypeDescriptor.GetComponentName(this, true);
    public TypeConverter? GetConverter() => TypeDescriptor.GetConverter(this, true);
    public EventDescriptor? GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);
    public PropertyDescriptor? GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);
    public object? GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);
    public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);
    public EventDescriptorCollection GetEvents(Attribute[]? attributes) => TypeDescriptor.GetEvents(this, attributes, true);

    public PropertyDescriptorCollection GetProperties() => GetProperties(null);

    public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        var properties = TypeDescriptor.GetProperties(this, attributes, true);
        var filtered = properties.Cast<PropertyDescriptor>().Where(prop =>
        {
            // Hide TradeCode when in LGPE mode
            if (prop.Name == nameof(TradeCode) && CurrentMode == ProgramMode.LGPE)
                return false;

            // Show LGPE codes only when in LGPE mode
            if (prop.Name == nameof(LGPECode1) && CurrentMode != ProgramMode.LGPE)
                return false;
            if (prop.Name == nameof(LGPECode2) && CurrentMode != ProgramMode.LGPE)
                return false;
            if (prop.Name == nameof(LGPECode3) && CurrentMode != ProgramMode.LGPE)
                return false;

            return true;
        }).ToArray();

        return new PropertyDescriptorCollection(filtered);
    }

    public object? GetPropertyOwner(PropertyDescriptor? pd) => this;
}
