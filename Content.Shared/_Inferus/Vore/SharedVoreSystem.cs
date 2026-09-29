using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Coordinates;

namespace Content.Shared._Inferus.Vore;

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

public abstract partial class SharedVoreSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VorePredatorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VorePredatorComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<VorePredatorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VorePredatorComponent, SwallowActionEvent>(OnSwallowAction);
        SubscribeLocalEvent<VorePredatorComponent, StomachEjectActionEvent>(OnEjectAction);
        SubscribeLocalEvent<VorePredatorComponent, VoreDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<VorePredatorComponent, GibbedBeforeDeletionEvent>(OnGibContents);
    }

    private void OnStartup(Entity<VorePredatorComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Stomach = _containerSystem.EnsureContainer<Container>(ent.Owner, VorePredatorComponent.StomachContainerId);
    }

    private void OnInit(Entity<VorePredatorComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.SwallowActionEntity, ent.Comp.SwallowAction);
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.EjectActionEntity, ent.Comp.EjectAction);
    }

    private void OnShutdown(Entity<VorePredatorComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SwallowActionEntity);
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.EjectActionEntity);
    }

    /// <summary>
    /// The vore action
    /// </summary>
    private void OnSwallowAction(Entity<VorePredatorComponent> ent, ref SwallowActionEvent args)
    {
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target))
            return;

        args.Handled = true;
        var target = args.Target;
        
        _popupSystem.PopupCoordinates(
            $"{MetaData(ent.Owner).EntityName} is trying to swallow you!",
            args.Target.ToCoordinates(),
            args.Target,
            PopupType.Large
        );
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.SwallowTime, new VoreDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true,
        });
    }
    
    private void OnEjectAction(Entity<VorePredatorComponent> ent, ref StomachEjectActionEvent args)
    {
        if (args.Handled) return;
        args.Handled = true;
        _containerSystem.EmptyContainer(ent.Comp.Stomach);
    }


    private void OnDoAfter(Entity<VorePredatorComponent> ent, ref VoreDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        _containerSystem.Insert(target, ent.Comp.Stomach);

        _audioSystem.PlayPredicted(ent.Comp.SoundSwallow, ent.Owner, ent.Owner);
    }

    private void OnGibContents(Entity<VorePredatorComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        _containerSystem.EmptyContainer(ent.Comp.Stomach);
    }
    
    public bool HasPrey(Entity<VorePredatorComponent?> pred, EntityUid? prey, [NotNullWhen(true)] out Container? stomach)
    {
        stomach = null;

        if (prey == null) return false;
        if (!Resolve(pred, ref pred.Comp, false)) return false;
        if (pred.Comp?.Stomach is null) return false;
        if (pred.Comp.Stomach.ContainedEntities.All(ent => ent != prey)) return false;
        
        stomach = pred.Comp.Stomach;
        return true;
    }
    
    public Entity<VorePredatorComponent>? TryGetPred(Entity<TransformComponent?, MetaDataComponent?> prey)
    {
        VorePredatorComponent? predComp = null;
        if (
            _containerSystem.TryGetContainingContainer(prey, out var container) &&
            container.ID == VorePredatorComponent.StomachContainerId &&
            Resolve(container.Owner, ref predComp, false)
        ) return new(container.Owner, predComp);
        return null;
    }

    public Entity<VorePredatorComponent>? TryGetPred(EntityUid prey)
    {
        TransformComponent? comp1 = null;
        MetaDataComponent? comp2 = null;
        Resolve(prey, ref comp1, ref comp2, false);
        return TryGetPred(new Entity<TransformComponent?, MetaDataComponent?>(prey, comp1, comp2));
    }
}

public sealed partial class SwallowActionEvent : EntityTargetActionEvent;

public sealed partial class StomachEjectActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class VoreDoAfterEvent : SimpleDoAfterEvent;

