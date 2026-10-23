using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server._LuaM.HoloComms;
using Content.Server.Power.EntitySystems;
using Content.Server.Telephone;
using Content.Shared._LuaM.HoloComms;
using Content.Shared.Holopad;
using Content.Shared.Telephone;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._LuaM;

/// <summary>
/// The comms of the shuttle consoles and the line that joins the comms points of a ship.
/// </summary>
[TestFixture]
public sealed class HoloCommsTest
{
    private const string Console = "ComputerShuttle";
    private const string ShipHolopad = "NFHolopadShip";

    // The power of the receivers is not worked out every tick
    private const int PowerTicks = 30;

    [Test]
    public async Task ShipLineTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.EntMan;
        var mapManager = server.ResolveDependency<IMapManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var xformSystem = entMan.System<SharedTransformSystem>();
        var power = entMan.System<PowerReceiverSystem>();
        var telephones = entMan.System<TelephoneSystem>();
        var line = entMan.System<ShipCommsLineSystem>();

        EntityUid gridA = default;
        EntityUid gridB = default;
        EntityUid consoleA = default;
        EntityUid consoleB = default;
        EntityUid holopadB = default;

        await server.WaitAssertion(() =>
        {
            gridA = map.Grid;
            var newGrid = mapManager.CreateGridEntity(map.MapId);
            gridB = newGrid.Owner;
            xformSystem.SetLocalPosition(gridB, new Vector2(40f, 0f));

            var tiles = new List<(Vector2i, Tile)>();
            for (var x = -3; x <= 3; x++)
            {
                for (var y = -3; y <= 3; y++)
                {
                    tiles.Add((new Vector2i(x, y), map.Tile.Tile));
                }
            }

            mapSystem.SetTiles(gridA, map.Grid.Comp, tiles);
            mapSystem.SetTiles(gridB, newGrid.Comp, tiles);

            consoleA = entMan.SpawnEntity(Console, new EntityCoordinates(gridA, 0.5f, 0.5f));
            consoleB = entMan.SpawnEntity(Console, new EntityCoordinates(gridB, 0.5f, 0.5f));
            holopadB = entMan.SpawnEntity(ShipHolopad, new EntityCoordinates(gridB, 2.5f, 2.5f));

            foreach (var uid in new[] { consoleA, consoleB, holopadB })
            {
                power.SetNeedsPower(uid, false);
            }
        });

        await server.WaitRunTicks(PowerTicks);

        Entity<TelephoneComponent> moduleA = default;
        Entity<TelephoneComponent> moduleB = default;
        Entity<TelephoneComponent> pointB = default;

        // Every console gets its hidden holopad
        await server.WaitAssertion(() =>
        {
            moduleA = GetModule(entMan, consoleA);
            moduleB = GetModule(entMan, consoleB);
            pointB = (holopadB, entMan.GetComponent<TelephoneComponent>(holopadB));

            Assert.Multiple(() =>
            {
                Assert.That(entMan.GetComponent<TransformComponent>(moduleA).ParentUid, Is.EqualTo(consoleA));
                Assert.That(entMan.GetComponent<HoloCommsModuleComponent>(moduleA).Console, Is.EqualTo(consoleA));
                Assert.That(telephones.IsTelephonePowered(moduleA), "The module runs on the power of its console");
            });

            // The other ship is listed once, with whichever of its points is free
            var contacts = new List<HoloCommsContact>();
            line.GetContacts(moduleA, contacts);
            var ships = contacts.Where(c => c.Kind == HoloCommsContactKind.Ship).ToList();

            Assert.That(ships, Has.Count.EqualTo(1));
            Assert.That(GetGrid(entMan, entMan.GetEntity(ships[0].Target)), Is.EqualTo(gridB));
            Assert.That(contacts.Any(c => GetGrid(entMan, entMan.GetEntity(c.Target)) == gridA), Is.False,
                "The console only talks to other ships");

            // Inside its own ship a holopad still sees the other holopads one by one, never the consoles
            contacts.Clear();
            line.GetContacts(pointB, contacts);
            Assert.That(contacts.Any(c => entMan.GetEntity(c.Target) == moduleB.Owner), Is.False);

            telephones.CallTelephone(moduleA, pointB, consoleA);
        });

        await server.WaitRunTicks(1);

        // A call to the ship rings every point of it
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(moduleA.Comp.CurrentState, Is.EqualTo(TelephoneState.Calling));
                Assert.That(pointB.Comp.CurrentState, Is.EqualTo(TelephoneState.Ringing));
                Assert.That(moduleB.Comp.CurrentState, Is.EqualTo(TelephoneState.Ringing));
            });

            telephones.AnswerTelephone(moduleB, consoleB);
        });

        await server.WaitRunTicks(1);

        // Once one point answers, every point of both ships is in the call
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(moduleA.Comp.CurrentState, Is.EqualTo(TelephoneState.InCall));
                Assert.That(moduleB.Comp.CurrentState, Is.EqualTo(TelephoneState.InCall));
                Assert.That(pointB.Comp.CurrentState, Is.EqualTo(TelephoneState.InCall));
                Assert.That(telephones.IsSourceConnectedToReceiver(moduleA, moduleB));
                Assert.That(telephones.IsSourceConnectedToReceiver(moduleA, pointB));
                Assert.That(entMan.GetComponent<HolopadComponent>(moduleA).Hologram, Is.Not.Null,
                    "The console shows the other side through a hologram");
            });

            // Hanging up anywhere hangs up the whole ship
            line.HangUp(pointB);

            Assert.Multiple(() =>
            {
                Assert.That(telephones.IsTelephoneEngaged(moduleA), Is.False);
                Assert.That(telephones.IsTelephoneEngaged(moduleB), Is.False);
                Assert.That(telephones.IsTelephoneEngaged(pointB), Is.False);
            });

            // The setting is the same on every point of the ship
            line.SetDoNotDisturb(pointB, true);

            Assert.Multiple(() =>
            {
                Assert.That(line.IsDoNotDisturb(moduleB), "Do not disturb is shared by the ship");
                Assert.That(line.IsDoNotDisturb(moduleA), Is.False, "Another ship keeps its own setting");
            });

            var contacts = new List<HoloCommsContact>();
            line.GetContacts(moduleA, contacts);
            Assert.That(contacts.Any(c => GetGrid(entMan, entMan.GetEntity(c.Target)) == gridB), Is.False,
                "A ship that takes no calls is not listed");

            // Even while the line still hangs up
            telephones.CallTelephone(moduleA, pointB, consoleA);

            Assert.Multiple(() =>
            {
                Assert.That(telephones.IsTelephoneEngaged(pointB), Is.False);
                Assert.That(pointB.Comp.CurrentState, Is.Not.EqualTo(TelephoneState.Ringing));
            });

            line.SetDoNotDisturb(moduleB, false);
            Assert.That(line.IsDoNotDisturb(pointB), Is.False);

            telephones.CallTelephone(moduleA, pointB, consoleA);
            Assert.That(pointB.Comp.CurrentState, Is.EqualTo(TelephoneState.Ringing));

            // No power at the console, no comms. The switch, the console would run on its battery otherwise
            power.SetPowerDisabled(consoleA, true);
        });

        await server.WaitRunTicks(PowerTicks);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(telephones.IsTelephonePowered(moduleA), Is.False);
                Assert.That(telephones.IsTelephoneEngaged(moduleA), Is.False, "The call drops with the power");
                Assert.That(telephones.IsTelephoneEngaged(pointB), Is.False);
                Assert.That(telephones.IsTelephoneEngaged(moduleB), Is.False);
            });
        });

        // A console placed on a map that is being mapped must not save a module into it
        var mapping = await pair.CreateTestMap(initialized: false);

        await server.WaitAssertion(() =>
        {
            var console = entMan.SpawnEntity(Console, mapping.GridCoords);

            Assert.That(entMan.GetComponent<HoloCommsConsoleComponent>(console).Module, Is.Null);

            var children = entMan.GetComponent<TransformComponent>(console).ChildEnumerator;
            while (children.MoveNext(out var child))
            {
                Assert.That(entMan.HasComponent<HoloCommsModuleComponent>(child), Is.False);
            }
        });

        await pair.CleanReturnAsync();
    }

    private static Entity<TelephoneComponent> GetModule(IEntityManager entMan, EntityUid console)
    {
        var module = entMan.GetComponent<HoloCommsConsoleComponent>(console).Module;
        Assert.That(module, Is.Not.Null, "The console has no comms module");
        return (module!.Value, entMan.GetComponent<TelephoneComponent>(module.Value));
    }

    private static EntityUid? GetGrid(IEntityManager entMan, EntityUid uid)
    {
        return entMan.GetComponent<TransformComponent>(uid).GridUid;
    }
}
