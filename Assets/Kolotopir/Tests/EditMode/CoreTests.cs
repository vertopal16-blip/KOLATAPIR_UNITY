using System.Collections.Generic;
using Kolotopir.Core;
using NUnit.Framework;

namespace Kolotopir.Tests
{
    /// <summary>Pruebas de los sistemas centrales (doc: "probar casos límite" de salud y daño, progresión, inventario...).</summary>
    public class VitalTests
    {
        [Test]
        public void HealthStartsAtHundredAsFloat()
        {
            var s = new GameState();
            Assert.AreEqual(100f, s.Health.Max, 0.0001f);
            Assert.AreEqual(100f, s.Health.Current, 0.0001f);
        }

        [Test]
        public void DamageNeverGoesBelowZero()
        {
            var v = new Vital(100f);
            float applied = v.Reduce(250.5f);
            Assert.AreEqual(100f, applied, 0.0001f);
            Assert.AreEqual(0f, v.Current, 0.0001f);
            Assert.IsTrue(v.IsEmpty);
        }

        [Test]
        public void HealingNeverExceedsMax()
        {
            var v = new Vital(100f);
            v.Reduce(30.25f);
            float healed = v.Restore(1000f);
            Assert.AreEqual(30.25f, healed, 0.0001f);
            Assert.AreEqual(100f, v.Current, 0.0001f);
        }

        [Test]
        public void NegativeAndNaNValuesAreIgnored()
        {
            var v = new Vital(100f);
            Assert.AreEqual(0f, v.Reduce(-5f));
            Assert.AreEqual(0f, v.Reduce(float.NaN));
            Assert.AreEqual(0f, v.Restore(-5f));
            Assert.AreEqual(100f, v.Current, 0.0001f);
        }

        [Test]
        public void DecimalDamageAccumulates()
        {
            var v = new Vital(100f);
            for (int i = 0; i < 10; i++) v.Reduce(0.5f);
            Assert.AreEqual(95f, v.Current, 0.001f);
        }

        [Test]
        public void RaisingMaxKeepsTheDifference()
        {
            var v = new Vital(100f);
            v.Reduce(40f);
            v.SetMax(120f);
            Assert.AreEqual(80f, v.Current, 0.0001f);
            v.SetMax(50f);
            Assert.AreEqual(50f, v.Current, 0.0001f);
        }

        [Test]
        public void DeadCharacterIsNotRevivedByMaxIncrease()
        {
            var v = new Vital(100f);
            v.Reduce(100f);
            v.SetMax(130f);
            Assert.AreEqual(0f, v.Current, 0.0001f);
        }
    }

    public class DamageTests
    {
        [Test]
        public void DefenseReducesPhysicalDamage()
        {
            float none = DamageModel.Compute(20f, DamageType.Fisico, 0f, 0f, 0f);
            float armored = DamageModel.Compute(20f, DamageType.Fisico, 20f, 0f, 0f);
            Assert.AreEqual(20f, none, 0.001f);
            Assert.Less(armored, none);
            Assert.Greater(armored, 0f);
        }

        [Test]
        public void ResistanceReducesItsOwnDamageType()
        {
            float raw = DamageModel.Compute(20f, DamageType.Resonancia, 0f, 0f, 0f);
            float resisted = DamageModel.Compute(20f, DamageType.Resonancia, 0f, 0.9f, 0.5f);
            Assert.AreEqual(10f, resisted, 0.001f);
            Assert.AreEqual(20f, raw, 0.001f);
        }

        [Test]
        public void ResistanceIsCappedAndDamageHasAMinimum()
        {
            float d = DamageModel.Compute(1f, DamageType.Fisico, 500f, 5f, 0f);
            Assert.AreEqual(DamageModel.MinimumDamage, d, 0.0001f);
        }

        [Test]
        public void PerfectBlockNegatesDamage()
        {
            Assert.AreEqual(0f, DamageModel.Blocked(30f, true));
            Assert.AreEqual(9f, DamageModel.Blocked(30f, false, 0.7f), 0.001f);
        }
    }

    public class ProgressionTests
    {
        [Test]
        public void LevelsUpAndGrantsPoints()
        {
            var p = new Progression();
            int needed = Progression.XpToNext(1);
            int gained = p.AddXp(needed);
            Assert.AreEqual(1, gained);
            Assert.AreEqual(2, p.Level);
            Assert.AreEqual(1, p.AttributePoints);
            Assert.AreEqual(1, p.SkillPoints);
        }

        [Test]
        public void CannotExceedMaxLevel()
        {
            var p = new Progression();
            p.AddXp(10000000);
            Assert.AreEqual(Progression.MaxLevel, p.Level);
            Assert.AreEqual(0, p.Xp);
            Assert.AreEqual(0, p.AddXp(500));
        }

        [Test]
        public void SpendingAttributePointRaisesHealth()
        {
            var s = new GameState();
            s.AddXp(Progression.XpToNext(1), "test");
            float before = s.Stats.MaxHealth;
            Assert.IsTrue(s.Progression.SpendAttributePoint(AttributeType.Vigor));
            s.RecomputeStats();
            Assert.AreEqual(before + 10f, s.Stats.MaxHealth, 0.001f);
            Assert.IsFalse(s.Progression.SpendAttributePoint(AttributeType.Vigor));
        }

        [Test]
        public void SkillTiersRequirePreviousSkills()
        {
            var s = new GameState();
            s.AddXp(Progression.XpToNext(1) + Progression.XpToNext(2) + Progression.XpToNext(3), "test");
            var tier2 = GameDb.Skill("contraataque");
            Assert.IsFalse(s.Skills.CanUnlock(tier2, s.Progression, GameDb.Skills, out _));
            Assert.IsTrue(s.Skills.Unlock(GameDb.Skill("piel_curtida"), s.Progression, GameDb.Skills));
            Assert.IsTrue(s.Skills.Unlock(tier2, s.Progression, GameDb.Skills));
        }
    }

    public class InventoryTests
    {
        [Test]
        public void StacksRespectMaxStack()
        {
            var s = new GameState();
            s.Inventory.Clear();
            s.Inventory.Add("venda", 25);
            Assert.AreEqual(25, s.Inventory.Count("venda"));
            int stacks = 0;
            foreach (var st in s.Inventory.Stacks) if (st.ItemId == "venda") stacks++;
            Assert.AreEqual(3, stacks);
            Assert.IsTrue(s.Inventory.Remove("venda", 24));
            Assert.AreEqual(1, s.Inventory.Count("venda"));
            Assert.IsFalse(s.Inventory.Remove("venda", 2));
        }

        [Test]
        public void UniqueItemsAreNotDuplicated()
        {
            var s = new GameState();
            s.Inventory.Add("espada_reverso");
            s.Inventory.Add("espada_reverso");
            Assert.AreEqual(1, s.Inventory.Count("espada_reverso"));
        }

        [Test]
        public void EquippingArmorRaisesDefenseAndSwapsBack()
        {
            var s = new GameState();
            s.Inventory.Add("chaleco_explorador");
            s.Inventory.Add("peto_guardian");
            Assert.IsTrue(s.Equipment.Equip(s.Inventory.FirstStack("chaleco_explorador"), s.Inventory, GameDb.Item));
            Assert.AreEqual(5f, s.Stats.Defense, 0.001f);
            Assert.IsTrue(s.Equipment.Equip(s.Inventory.FirstStack("peto_guardian"), s.Inventory, GameDb.Item));
            Assert.AreEqual(9f, s.Stats.Defense, 0.001f);
            Assert.AreEqual(1, s.Inventory.Count("chaleco_explorador"));
            Assert.Less(s.Stats.MoveSpeedMult, 1f);
        }

        [Test]
        public void TwoAccessorySlotsAreUsed()
        {
            var s = new GameState();
            s.NewGame();
            s.Inventory.Add("colgante_fragmento");
            Assert.IsTrue(s.Equipment.Equip(s.Inventory.FirstStack("colgante_fragmento"), s.Inventory, GameDb.Item));
            Assert.IsNotNull(s.Equipment.Get(EquipSlot.Accesorio1));
            Assert.IsNotNull(s.Equipment.Get(EquipSlot.Accesorio2));
        }

        [Test]
        public void WeaponUpgradeIsExclusive()
        {
            var s = new GameState();
            s.Inventory.Add("espada_reverso");
            s.Inventory.Add("lasca", 3);
            s.Inventory.Add("resina", 1);
            s.Inventory.Add("cuero", 1);
            s.Inventory.Add("fibra", 2);
            var sword = s.Inventory.FirstStack("espada_reverso");
            var def = GameDb.Item("espada_reverso");
            Assert.IsTrue(Crafting.ApplyUpgrade(sword, def.Upgrades[0], s));
            Assert.IsFalse(Crafting.ApplyUpgrade(sword, def.Upgrades[1], s));
            s.Equipment.Equip(sword, s.Inventory, GameDb.Item);
            Assert.AreEqual(16f, s.Stats.Attack, 0.001f);
        }

        [Test]
        public void CraftingNeedsKnowledgeAndMaterials()
        {
            var s = new GameState();
            var recipe = new List<RecipeDef>(GameDb.Recipes).Find(r => r.Id == "r_unguento");
            s.Inventory.Add("hierba", 2);
            s.Inventory.Add("resina", 1);
            Assert.IsFalse(Crafting.Craft(recipe, s, true));
            s.Flags.Set("receta_basica");
            Assert.IsTrue(Crafting.Craft(recipe, s, false));
            Assert.AreEqual(1, s.Inventory.Count("unguento"));
            Assert.AreEqual(0, s.Inventory.Count("hierba"));
        }

        [Test]
        public void BarterRequiresEnoughValue()
        {
            var s = new GameState();
            var merchant = GameDb.Merchant("boro");
            var offer = merchant.Offers.Find(o => o.ItemId == "tonico_bruma");
            s.Inventory.Add("fibra", 3);
            var cheap = new List<ItemAmount> { new ItemAmount("fibra", 3) };
            Assert.IsFalse(Barter.CanTrade(cheap, offer, s, merchant));
            s.Inventory.Add("fragmento", 3);
            var fair = new List<ItemAmount> { new ItemAmount("fragmento", 3) };
            Assert.IsTrue(Barter.Trade(fair, offer, s, merchant));
            Assert.AreEqual(1, s.Inventory.Count("tonico_bruma"));
        }

        [Test]
        public void QuestItemsCannotBeBartered()
        {
            var s = new GameState();
            var merchant = GameDb.Merchant("boro");
            s.Inventory.Add("cuchara_doblada");
            var offered = new List<ItemAmount> { new ItemAmount("cuchara_doblada", 1) };
            Assert.IsFalse(Barter.CanTrade(offered, merchant.Offers[0], s, merchant));
        }
    }

    public class QuestAndNotebookTests
    {
        [Test]
        public void NewGameStartsFirstChapter()
        {
            var s = new GameState();
            s.NewGame();
            Assert.IsTrue(s.QuestActive("mq_silencio"));
            Assert.IsTrue(s.AtStep("mq_silencio", "hablar_gina"));
            Assert.IsTrue(s.Equipment.IsEquipped("amuleto_familiar"));
            Assert.AreEqual(2, s.Attr(AttributeType.Resonancia));
        }

        [Test]
        public void StepsAdvanceInChainWhenConditionsAlreadyMet()
        {
            var s = new GameState();
            s.NewGame();
            var ctx = new CoreGameContext(s);
            s.Flags.Set("gina_intro_done");
            s.Flags.Set("heard_res_huellas_capsula");
            s.Quests.Evaluate(s, ctx);
            Assert.IsTrue(s.AtStep("mq_silencio", "taller"));
            Assert.Greater(s.Progression.TotalXp, 0);
        }

        [Test]
        public void HypothesisCanBeChosenAndResolved()
        {
            var s = new GameState();
            s.NewGame();
            var ctx = new CoreGameContext(s);
            ctx.AddEvidence("ev_recuerdo_custodio");
            Assert.IsTrue(s.Notebook.ChooseHypothesis("mis_padres", "h_ambas"));
            ctx.AddEvidence("ev_recuerdo_completo");
            Assert.IsTrue(s.Notebook.IsResolved("mis_padres"));
            Assert.AreEqual(Knowledge.Sabe, s.Notebook.Status("mis_padres"));
            Assert.IsFalse(s.Notebook.ChooseHypothesis("mis_padres", "h_voluntad"));
        }

        [Test]
        public void EvidenceCanRefuteAHypothesis()
        {
            var s = new GameState();
            var ctx = new CoreGameContext(s);
            var m = GameDb.Mystery("mis_padres");
            var secuestro = m.Hypotheses.Find(h => h.Id == "h_secuestro");
            ctx.AddEvidence("ev_recuerdo_custodio");
            Assert.AreEqual(1, s.Notebook.HypothesisSupport(secuestro));
            ctx.AddEvidence("ev_boro_testimonio");
            Assert.AreEqual(-1, s.Notebook.HypothesisSupport(secuestro));
        }

        [Test]
        public void KnowledgeOnlyGoesUp()
        {
            var s = new GameState();
            Assert.IsTrue(s.Notebook.AddEntry("cul_liga", Knowledge.Observado));
            Assert.IsFalse(s.Notebook.AddEntry("cul_liga", Knowledge.Sospecha));
            Assert.IsTrue(s.Notebook.AddEntry("cul_liga", Knowledge.Sabe));
            Assert.AreEqual(Knowledge.Sabe, s.Notebook.Status("cul_liga"));
        }

        [Test]
        public void DialogueRunnerFollowsChoicesAndEffects()
        {
            var s = new GameState();
            s.NewGame();
            var ctx = new CoreGameContext(s);
            var runner = new DialogueRunner();
            Assert.IsTrue(runner.Start(GameDb.Dialogue("gina_intro"), ctx));
            int guard = 0;
            while (!runner.Finished && guard++ < 50)
            {
                var choices = runner.VisibleChoices();
                if (choices.Count > 0) runner.Choose(choices[choices.Count - 1]);
                else runner.Continue();
            }
            Assert.IsTrue(runner.Finished);
            Assert.IsTrue(s.Has("gina_intro_done"));
        }

        [Test]
        public void LockedChoicesStayLocked()
        {
            var s = new GameState();
            s.NewGame();
            var ctx = new CoreGameContext(s);
            s.Quests.Start("sq_tome");
            var runner = new DialogueRunner();
            runner.Start(GameDb.Dialogue("tome_entrega"), ctx);
            var choices = runner.VisibleChoices();
            Assert.AreEqual(2, choices.Count);
            Assert.IsTrue(runner.IsLocked(choices[0]));
            Assert.IsFalse(runner.Choose(choices[0]));
        }

        [Test]
        public void PrivateMemoryWithoutConsentHurtsRelationship()
        {
            var s = new GameState();
            var ctx = new CoreGameContext(s);
            GameDb.Resonance("res_boro_cuenta").OnHeard(ctx);
            Assert.Less(s.Affinity("boro"), 0);
            Assert.IsTrue(s.Remembers("boro", "invadio_privacidad"));
        }

        [Test]
        public void PrivateMemoryWithConsentImprovesRelationship()
        {
            var s = new GameState();
            var ctx = new CoreGameContext(s);
            s.Flags.Set("consent_boro_cuenta");
            GameDb.Resonance("res_boro_cuenta").OnHeard(ctx);
            Assert.Greater(s.Affinity("boro"), 0);
        }

        [Test]
        public void CapsuleDecisionHasGlobalConsequence()
        {
            var s = new GameState();
            var ctx = new CoreGameContext(s);
            var cap = GameDb.Capsule("cap_parada");
            var seal = cap.Decisions.Find(d => d.Id == "inaccesible");
            seal.Effect(ctx);
            Assert.AreEqual(ConsequenceScale.Global, seal.Scale);
            Assert.AreEqual(CapsuleState.Sellada, s.CapsuleOf("cap_sellada"));
        }
    }

    public class SaveTests
    {
        [Test]
        public void SaveRoundTripKeepsProgress()
        {
            var s = new GameState();
            s.NewGame();
            var ctx = new CoreGameContext(s);
            s.AddXp(400, "test");
            s.Progression.SpendAttributePoint(AttributeType.Fuerza);
            ctx.GiveItem("espada_reverso");
            s.Equipment.Equip(s.Inventory.FirstStack("espada_reverso"), s.Inventory, GameDb.Item);
            s.Health.Reduce(12.5f);
            ctx.SetFlag("gina_intro_done");
            ctx.AddEvidence("ev_huellas");
            s.Notebook.ChooseHypothesis("mis_padres", "h_voluntad");
            s.World.MarkTaken("c_mojon");
            s.World.Capsules["cap_parada"] = CapsuleState.Estable;
            s.Relationships.Change("boro", 12, "ayudo_coleccion");
            s.Quests.Evaluate(s, ctx);

            var data = SaveConverter.Capture(s, 10f, 20f, 30f, 45f, "Prados", "test");
            var loaded = new GameState();
            SaveConverter.Apply(data, loaded);

            Assert.AreEqual(s.Progression.Level, loaded.Progression.Level);
            Assert.AreEqual(s.Progression.Get(AttributeType.Fuerza), loaded.Progression.Get(AttributeType.Fuerza));
            Assert.IsTrue(loaded.Equipment.IsEquipped("espada_reverso"));
            Assert.AreEqual(s.Health.Current, loaded.Health.Current, 0.001f);
            Assert.IsTrue(loaded.Has("gina_intro_done"));
            Assert.IsTrue(loaded.HasEvidence("ev_huellas"));
            Assert.AreEqual("h_voluntad", loaded.Hypothesis("mis_padres"));
            Assert.IsTrue(loaded.World.IsTaken("c_mojon"));
            Assert.AreEqual(CapsuleState.Estable, loaded.CapsuleOf("cap_parada"));
            Assert.AreEqual(12, loaded.Affinity("boro"));
            Assert.AreEqual(s.Quests.StepIndex("mq_silencio"), loaded.Quests.StepIndex("mq_silencio"));
        }

        [Test]
        public void DeathDoesNotLoseItemsByDefault()
        {
            // Doc: al llegar a cero salud, Nilo reaparece sin pérdida de objetos por defecto.
            var s = new GameState();
            s.NewGame();
            int vendas = s.Inventory.Count("venda");
            s.Health.Reduce(1000f);
            Assert.IsTrue(s.Health.IsEmpty);
            s.Health.Fill();
            Assert.AreEqual(vendas, s.Inventory.Count("venda"));
        }
    }

    public class ContentTests
    {
        [Test]
        public void ContentReferencesAreValid()
        {
            var problems = GameDb.Validate();
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void EveryNpcHasADialogueForEveryState()
        {
            var s = new GameState();
            s.NewGame();
            foreach (var npc in GameDb.Npcs.Values)
            {
                string id = npc.SelectDialogue(s);
                Assert.IsNotNull(GameDb.Dialogue(id), $"{npc.Id} -> {id}");
            }
        }

        [Test]
        public void SchedulesCoverTheWholeDay()
        {
            foreach (var npc in GameDb.Npcs.Values)
                for (float h = 0f; h < 24f; h += 0.5f)
                {
                    bool covered = false;
                    foreach (var e in npc.Schedule) if (e.Contains(h)) covered = true;
                    Assert.IsTrue(covered, $"{npc.Id} sin rutina a las {h}");
                }
        }

        [Test]
        public void BetweenFiveAndTenNpcsWithRoutines()
        {
            Assert.That(GameDb.Npcs.Count, Is.InRange(5, 10));
        }

        [Test]
        public void CapsulePuzzleOrderMatchesCulturalOrder()
        {
            var cap = GameDb.Capsule("cap_parada");
            var symbols = new List<string>();
            foreach (var id in cap.CorrectOrder)
                symbols.Add(cap.Fragments.Find(f => f.Id == id).Symbol);
            CollectionAssert.AreEqual(new[] { "Agua", "Raíz", "Piedra", "Cielo" }, symbols);
        }
    }

    public class WorldTests
    {
        [Test]
        public void SettlementIsOnThePlateau()
        {
            Assert.AreEqual(Region.TribuDelReverso, WorldShape.RegionAt(WorldLayout.Plaza.X, WorldLayout.Plaza.Z));
            Assert.Greater(WorldShape.Height(WorldLayout.Plaza.X, WorldLayout.Plaza.Z), 40f);
        }

        [Test]
        public void ChasmSeparatesTribeAndMeadows()
        {
            var mid = P2.Lerp(WorldShape.BridgeWest, WorldShape.BridgeEast, 0.5f);
            Assert.AreEqual(Region.Abismo, WorldShape.RegionAt(mid.X, mid.Z));
            Assert.Less(WorldShape.Height(mid.X, mid.Z), 0f);
            Assert.Greater(WorldShape.Height(WorldShape.BridgeWest.X, WorldShape.BridgeWest.Z), 30f);
            Assert.Greater(WorldShape.Height(WorldShape.BridgeEast.X, WorldShape.BridgeEast.Z), 25f);
        }

        [Test]
        public void LakesHoldWaterAndTheIsletIsDry()
        {
            var lake = WorldShape.Lakes[0];
            var p = new P2(lake.Center.X + lake.Radius * 0.5f, lake.Center.Z);
            Assert.Less(WorldShape.Height(p.X, p.Z), lake.WaterLevel);
            Assert.Greater(WorldShape.Height(lake.Center.X, lake.Center.Z), lake.WaterLevel);
        }

        [Test]
        public void ImportantPlacesAreOnSolidGround()
        {
            void Check(P2 p, string id)
            {
                if (WorldShape.IsOnBridgeCorridor(p.X, p.Z)) return; // el puente colgante cruza el abismo a propósito
                Assert.Greater(WorldShape.Height(p.X, p.Z), 0f, id);
                Assert.IsTrue(WorldShape.InWorld(p.X, p.Z), id);
            }
            foreach (var l in WorldLayout.Locations) Check(l.Pos, l.Id);
            foreach (var n in WorldLayout.Nodes) Check(n.Pos, n.Id);
            foreach (var r in WorldLayout.RestPoints) Check(r.Pos, r.Id);
            foreach (var c in WorldLayout.Chests) Check(c.Pos, c.Id);
            foreach (var sp in WorldLayout.Spawns) Check(sp.Pos, sp.Id);
            foreach (var pk in WorldLayout.Pickups) Check(pk.Pos, pk.Id);
            foreach (var rs in WorldLayout.ResonanceSpots) Check(rs.Pos, rs.ResonanceId);
            foreach (var ex in GameDb.Examines.Values) Check(ex.Pos, ex.Id);
        }

        [Test]
        public void SettlementNodesAreNotUnderwaterAndSafe()
        {
            foreach (var r in WorldLayout.RestPoints)
                Assert.IsFalse(WorldShape.IsUnderwater(r.Pos.X, r.Pos.Z), r.Id);
            Assert.IsTrue(WorldShape.IsSafeGround(WorldLayout.Plaza.X, WorldLayout.Plaza.Z));
            Assert.IsFalse(WorldShape.IsSafeGround(300f, 300f) && !WorldShape.IsOnBridgeCorridor(300f, 300f));
        }

        [Test]
        public void NavGraphIsConnectedWithinEachArea()
        {
            var adj = new Dictionary<string, List<string>>();
            foreach (var n in WorldLayout.Nodes) adj[n.Id] = new List<string>();
            foreach (var e in WorldLayout.Edges) { adj[e.Key].Add(e.Value); adj[e.Value].Add(e.Key); }
            HashSet<string> Reach(string start)
            {
                var seen = new HashSet<string> { start };
                var q = new Queue<string>();
                q.Enqueue(start);
                while (q.Count > 0) foreach (var nb in adj[q.Dequeue()]) if (seen.Add(nb)) q.Enqueue(nb);
                return seen;
            }
            var tribe = Reach("plaza");
            foreach (var id in new[] { "gina_bed", "oren_bed", "haro_bed", "lia_bed", "tome_bed", "suri_bed", "guard_dummy", "stones", "tree_seat", "fields_3", "gate_in", "workshop", "store" })
                Assert.IsTrue(tribe.Contains(id), id);
            var prados = Reach("boro_camp");
            foreach (var id in new[] { "boro_tent", "boro_fire", "hill_top", "hill_top2", "bc_beds", "bc_fire", "herbs", "herbs2", "ruins_n", "m7" })
                Assert.IsTrue(prados.Contains(id), id);
        }

        [Test]
        public void SplatWeightsAreNormalized()
        {
            var w = new float[4];
            for (int i = 0; i < 50; i++)
            {
                float x = 20f + i * 19f, z = 30f + i * 17f;
                WorldShape.SplatWeights(x, z, w);
                Assert.AreEqual(1f, w[0] + w[1] + w[2] + w[3], 0.001f);
            }
        }

        [Test]
        public void SectorsCoverTheWorld()
        {
            WorldShape.SectorOf(0f, 0f, out int sx, out int sz);
            Assert.AreEqual(0, sx);
            WorldShape.SectorOf(999f, 999f, out sx, out sz);
            Assert.AreEqual(WorldShape.SectorsPerSide - 1, sx);
            Assert.AreEqual(WorldShape.SectorsPerSide - 1, sz);
        }
    }
}
