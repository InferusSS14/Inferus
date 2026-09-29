namespace Content.Shared._Inferus.Vore;

using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedVoreSystem))]
public sealed partial class VorePredatorComponent : Component
{
    [DataField]
    public EntProtoId SwallowAction = "ActionSwallow";

    [DataField, AutoNetworkedField]
    public EntityUid? SwallowActionEntity;
    
    [DataField]
    public EntProtoId EjectAction = "ActionStomachEject";

    [DataField, AutoNetworkedField]
    public EntityUid? EjectActionEntity;

    /// <summary>
    /// The amount of time it takes to swallow something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SwallowTime = 6f;

    /// <summary>
    /// The sound to play when finishing swallowing something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundSwallow = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// The container to store the swallowed entities in.
    /// </summary>
    [ViewVariables]
    public static string StomachContainerId = "stomach";

    /// <summary>
    /// Where the entities go when it swallows them, empties when it is butchered.
    /// </summary>
    [ViewVariables]
    public Container Stomach = default!;

    /// <summary>
    /// Determines what things the predator can swallow.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "MobState",
        }
    };
}

