using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared._Inferus.Vore;
using Content.Shared.Temperature.Components;

namespace Content.Server._Inferus.Vore;

public sealed partial class VoreSystem: SharedVoreSystem
{
    [Dependency] private TemperatureSystem _temperatureSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VorePredatorComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
    }

    private void OnAtmosExposedUpdate(Entity<VorePredatorComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        if (!TryComp(ent.Owner, out TemperatureComponent? predTemp)) return;
        float initialPredTemp = predTemp.CurrentTemperature;
        foreach (EntityUid prey in ent.Comp.Stomach.ContainedEntities)
        {
            if (!TryComp(prey, out TemperatureComponent? preyTemp)) continue;
            // The temperature delta between the two, in Kelvin
            var temperatureDelta = initialPredTemp - preyTemp.CurrentTemperature;
            // Specific heat capacity for each entity
            var heatCapacityPred = _temperatureSystem.GetHeatCapacity(ent.Owner, predTemp);
            var heatCapacityPrey = _temperatureSystem.GetHeatCapacity(prey, preyTemp);
            var totalHeatCapacity = heatCapacityPred + heatCapacityPrey;

            var predTempChange = -temperatureDelta * heatCapacityPrey / totalHeatCapacity * predTemp.AtmosTemperatureTransferEfficiency;
            var preyTempChange = temperatureDelta * heatCapacityPred / totalHeatCapacity * preyTemp.AtmosTemperatureTransferEfficiency;

            _temperatureSystem.ChangeHeat(prey, preyTempChange, temperature: preyTemp);
            _temperatureSystem.ChangeHeat(ent.Owner, predTempChange, temperature: predTemp);
        }
    }
}