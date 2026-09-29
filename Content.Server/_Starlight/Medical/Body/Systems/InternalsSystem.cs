using Content.Server.Atmos.EntitySystems;
using Content.Shared._Inferus.Vore;
using Content.Shared._Starlight.Medical.Body.Systems;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Internals;
using Content.Shared.Roles;
using Robust.Server.Containers;

namespace Content.Server._Starlight.Medical.Body.Systems;

public sealed partial class InternalsSystem : SharedInternalsSystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private GasTankSystem _gasTank = default!;
    [Dependency] private RespiratorSystem _respirator = default!;
    [Dependency] private ContainerSystem _container = default!;

    private EntityQuery<InternalsComponent> _internalsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _internalsQuery = GetEntityQuery<InternalsComponent>();

        SubscribeLocalEvent<InternalsComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<InternalsComponent, StartingGearEquippedEvent>(OnStartingGear);
    }

    private void OnStartingGear(EntityUid uid, InternalsComponent component, ref StartingGearEquippedEvent args)
    {
        if (component.BreathTools.Count == 0)
            return;

        if (component.GasTankEntity != null)
            return; // already connected

        // Can the entity breathe the air it is currently exposed to?
        if (_respirator.CanMetabolizeInhaledAir(uid))
            return;

        var tank = FindBestGasTank(uid);
        if (tank == null)
            return;

        // Could the entity metabolise the air in the linked gas tank?
        if (!_respirator.CanMetabolizeInhaledAir(uid, tank.Value.Comp.Air))
            return;

        ToggleInternals(uid, uid, force: false, component, ToggleMode.On);
    }

    private void OnInhaleLocation(Entity<InternalsComponent> ent, ref InhaleLocationEvent args)
    {
        // Inferus - check for pred internals, we essentially have to rewrite this entire function
        TransformComponent? comp1 = null;
        MetaDataComponent? comp2 = null;
        InternalsComponent? predInternals = null;
        Entity<InternalsComponent>? maybeRoot = ent;
        int iterations = 0;
        while (maybeRoot is { } root)
        {
            iterations++;
            if (iterations > 5)
            {
                Log.Warning($"Maximum iterations reached while resolving internals for entity {ent} (root: {root})");
                break;
            }
            if (AreInternalsWorking(root))
            {
                var gasTank = Comp<GasTankComponent>(root.Comp.GasTankEntity!.Value);
                args.Gas = _gasTank.RemoveAirVolume((root.Comp.GasTankEntity.Value, gasTank), args.Respirator.BreathVolume);
                // TODO: Should listen to gas tank updates instead I guess?
                _alerts.ShowAlert(ent.Owner, ent.Comp.InternalsAlert, GetSeverity(ent));
                break;
            }
            Resolve(ent.Owner, ref comp1, ref comp2, false);
            maybeRoot = null;
            if (
                _container.TryGetContainingContainer(new(ent.Owner, comp1, comp2), out var container) &&
                container.ID == VorePredatorComponent.StomachContainerId &&
                Resolve(container.Owner, ref predInternals, false)
            ) maybeRoot = new(container.Owner, predInternals);
            comp1 = null;
            comp2 = null;
            predInternals = null;
        }
    }
}
