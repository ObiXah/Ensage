﻿
using System;
using System.Collections.Generic;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using SharpDX;
using SharpDX.Direct3D9;

namespace ZeusSharp
{
    internal class Program
    {
        private static Item orchid, sheepstick, veil, soulring, arcane, blink, shiva, dagon, refresher, ethereal, halberd, bloodthorn;
        private static bool drawStealNotice;
        private static bool menuadded;
        private static ParticleEffect effect;
        private static bool statechanged;
        private static float Wdrawn, Qdrawn;
        private static float realWrange;
        private static int blinkdrawnr;
        private static Font _text;
        private static Font _notice;
        private static Line _line;
        private static string steallableHero;
        private static string heronametargeted;
        private static Hero target;
        private static Hero me;
        private static Hero vhero;
        private static string map;
        private static AbilityToggler menuValue;
        private static Menu Menu;
        private static readonly Dictionary<int, ParticleEffect> Effect = new Dictionary<int, ParticleEffect>();

        private static void Main()
        {
            Events.OnLoad += On_Load;
            Events.OnClose += On_Close;
            _text = new Font(
                Drawing.Direct3DDevice9,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 17,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearType
                });

            _notice = new Font(
                Drawing.Direct3DDevice9,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 30,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearType
                });

            _line = new Line(Drawing.Direct3DDevice9);
            Game.OnUpdate += Killsteal;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Drawing.OnDraw += Drawing_OnDraw;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            
        }

        private static void On_Load(object sender, EventArgs e)
        {
            if (me.ClassID == ClassID.CDOTA_Unit_Hero_Zuus)
            {
                if (!menuadded)
                {
                    InitMenu();
                    menuadded = true;
                }
                statechanged = true;
                map = Game.ShortLevelName;
            }
        }

        private static void On_Close(object sender, EventArgs e)
        {
                if (menuadded) Menu.RemoveFromMainMenu();
                if (Effect.TryGetValue(1, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(1);
                }
                if (Effect.TryGetValue(2, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(2);
                }
                if (Effect.TryGetValue(3, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(3);
                }
                menuadded = false;
        }

        private static void InitMenu()
        {
            var itemdict = new Dictionary<string, bool>
                           {
                               { "item_veil_of_discord", true }, { "item_shivas_guard", true},
                               { "item_sheepstick", true }, { "item_orchid", true }, { "item_bloodthorn", true }, { "item_dagon_5", true }, { "item_heavens_halberd", true },
                               { "item_ethereal_blade", true}
                           };
            Menu = new Menu("Zeus#", "Zeus#", true, "npc_dota_hero_zuus", true);
            var comboMenu = new Menu("Combo Tweaks", "combomenu", false, @"..\other\statpop_exclaim", true);
            comboMenu.AddItem(new MenuItem("enabledAbilities", "Items:").SetValue(new AbilityToggler(itemdict)));
            comboMenu.AddItem(
                new MenuItem("blink", "Use Blink").SetValue(true)
                    .SetTooltip("Blinks to target but not closer than specified range."));
            comboMenu.AddItem(
                new MenuItem("refresherToggle", "Use Refresher").SetValue(false)
                    .SetTooltip("Auto use refresher for 2x ultimate."));
            comboMenu.AddItem(
                new MenuItem("arcaneauto", "Auto Arcane Boots").SetValue(false)
                    .SetTooltip("Auto use arcane boots when off CD and mana wouldn't be wasted."));
            comboMenu.AddItem(
                new MenuItem("targetsearchrange", "Target Search Range").SetValue(new Slider(1000, 128, 2500))
                    .SetTooltip("Radius of target search range around cursor."));
            comboMenu.AddItem(
                new MenuItem("saferange", "Blink not closer than").SetValue(new Slider(650, 125, 850))
                    .SetTooltip(
                        "Increases combo range with blink. P.S. No point in blinking in melee to da face. Shoutout to Evervolv1337 ;)"));
            comboMenu.AddItem(
                new MenuItem("Wrealrange", "W Search Radius").SetValue(new Slider(300, 0, 375))
                    .SetTooltip("Try to W ground close to enemy giving +375 max range. Reduce range in case of misses."));

            var stealMenu = new Menu("Ultimate Usage", "stealmenu", false, "zuus_thundergods_wrath", true);
            stealMenu.AddItem(new MenuItem("stealToggle", "Auto Steal").SetValue(new KeyBind(45, KeyBindType.Toggle)).SetTooltip("Auto R on killable."));
            stealMenu.AddItem(
                new MenuItem("confirmSteal", "Manual Steal Key").SetValue(new KeyBind('F', KeyBindType.Press))
                    .SetTooltip("Manual R steal key."));
            stealMenu.AddItem(
                new MenuItem("useRincombo", "Don't steal with R in combo").SetValue(true)
                    .SetTooltip("Use R steal only when NOT in combo."));
            stealMenu.AddItem(new MenuItem("stealEdmg", "Try to add E dmg if possible").SetValue(true));

            var drawMenu = new Menu("Drawings", "drawmenu", false, @"..\other\statpop_star", true);
            drawMenu.AddItem(
                new MenuItem("drawblinkrange", "Draw Combo Blink Range").SetValue(true)
                    .SetTooltip("Uses blink range + safe range."));
            drawMenu.AddItem(new MenuItem("drawQrange", "Draw Q Range").SetValue(true).SetTooltip("Useful for farming."));
            drawMenu.AddItem(
                new MenuItem("drawWrange", "Draw W Real Range").SetValue(true).SetTooltip("Uses W non-targeting range."));
            drawMenu.AddItem(
                new MenuItem("drawblinkready", "Glow When Blink Off CD").SetValue(false)
                    .SetTooltip("Draw glow on zeus when blink dagger is off cooldown."));
            drawMenu.AddItem(
                new MenuItem("drawtargetglow", "Draw Glow On Target").SetValue(false)
                    .SetTooltip("Draw glow on selected target."));

            Menu.AddItem(
                new MenuItem("active", "Combo Key").SetValue(new KeyBind(32, KeyBindType.Press))
                    .SetTooltip("Hold this key for combo."));
            Menu.AddItem(
                new MenuItem("harass", "Harass Key").SetValue(new KeyBind('D', KeyBindType.Press))
                    .SetTooltip("Hold this key for harass. Not uses blink, refresher, hex, halberd, shiva."));
            Menu.AddItem(
                new MenuItem("qFarm", "Farm Key").SetValue(new KeyBind('E', KeyBindType.Press))
                    .SetTooltip("Hold this key to farm with Q."));
            Menu.AddItem(
                new MenuItem("wFarm", "Lasthit with W").SetValue(true)
                    .SetTooltip("Siege, neutrals, forge spirits, Lone Druid bear"));
            Menu.AddSubMenu(comboMenu);
            Menu.AddSubMenu(stealMenu);
            Menu.AddSubMenu(drawMenu);
            Menu.AddToMainMenu();
            menuValue = Menu.Item("enabledAbilities").GetValue<AbilityToggler>();
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            float qDmg, wDmg, eDmg, rDmg;

            me = ObjectManager.LocalHero;
            if (!menuadded) return;

            qDmg = me.Spellbook.SpellQ.GetDamage(me.Spellbook.SpellQ.Level - 1);
            wDmg = me.Spellbook.SpellW.GetDamage(me.Spellbook.SpellW.Level - 1);
            eDmg = me.Spellbook.SpellE.AbilitySpecialData.FirstOrDefault(x => x.Name == "damage_health_pct").GetValue(me.Spellbook.SpellE.Level - 1) / 100 * GetMyTotalSpellAmp();
            rDmg = me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").AbilitySpecialData.FirstOrDefault(x => x.Name == "damage").GetValue(me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").Level - 1);

            //Console.WriteLine("Q DMG: " + qDmg);
            //Console.WriteLine("W DMG: " + wDmg);
            //Console.WriteLine("E DMG: " + eDmg);
            //Console.WriteLine("R DMG: " + rDmg);

            realWrange = me.Spellbook.SpellW.GetCastRange() + Menu.Item("Wrealrange").GetValue<Slider>().Value;

            target = me.ClosestToMouseTarget(Menu.Item("targetsearchrange").GetValue<Slider>().Value);
            if (target != null && target.IsMagicImmune())
            {
                var enemylist2 =
                    ObjectManager.GetEntities<Hero>()
                        .Where(
                            e =>
                                e.Team != me.Team && e.IsAlive && e.IsVisible && !e.IsIllusion &&
                                !e.UnitState.HasFlag(UnitState.MagicImmune) && me.Distance2D(e) < realWrange);
                if (enemylist2.Count() != 0) target = enemylist2.MinOrDefault(x => x.Health);
            }

            var enemylist =
                ObjectManager.GetEntities<Hero>()
                    .Where(
                        e =>
                            e.Team != me.Team && e.IsAlive && e.IsVisible && !e.IsIllusion &&
                            !e.UnitState.HasFlag(UnitState.MagicImmune) && e.IsChanneling());
            foreach (var channeling in enemylist)
            {
                if (me.Distance2D(channeling) < realWrange && channeling.GetChanneledAbility().ChannelTime() > 1)
                    target = channeling;
            }

            // Items
            orchid = me.FindItem("item_orchid");
            bloodthorn = me.FindItem("item_bloodthorn");
            sheepstick = me.FindItem("item_sheepstick");
            veil = me.FindItem("item_veil_of_discord");
            soulring = me.FindItem("item_soul_ring");
            arcane = me.FindItem("item_arcane_boots");
            blink = me.FindItem("item_blink");
            shiva = me.FindItem("item_shivas_guard");
            halberd = me.FindItem("item_heavens_halberd");
            dagon = me.Inventory.Items.FirstOrDefault(item => item.Name.Contains("item_dagon"));
            refresher = me.FindItem("item_refresher");
            ethereal = me.FindItem("item_ethereal_blade");

            var refresherComboManacost = me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").ManaCost + me.Spellbook.Spell2.ManaCost + me.Spellbook.Spell1.ManaCost;

            // Manacost calculation
            if (veil != null)
                refresherComboManacost += veil.ManaCost;

            if (orchid != null)
                refresherComboManacost += orchid.ManaCost;

            if (sheepstick != null)
                refresherComboManacost += sheepstick.ManaCost;

            if (shiva != null)
                refresherComboManacost += shiva.ManaCost;

            if (halberd  != null)
                refresherComboManacost += halberd.ManaCost;

            if (dagon != null)
                refresherComboManacost += dagon.ManaCost;

            if (ethereal != null)
                refresherComboManacost += ethereal.ManaCost;

            if (refresher != null)
                refresherComboManacost += refresher.ManaCost;

            if (arcane != null && arcane.CanBeCasted() && me.MaximumMana > me.Mana + 135 && Utils.SleepCheck("arcane") && Menu.Item("arcaneauto").GetValue<bool>())
            {
                arcane.UseAbility();
                Utils.Sleep(Game.Ping+150, "arcane");
            }

            var creepQ =
                ObjectManager.GetEntities<Unit>()
                    .Where(
                        creep =>
                            (creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane ||
                            creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                            creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral ||
                            creep.ClassID == ClassID.CDOTA_Unit_SpiritBear ||
                            creep.ClassID == ClassID.CDOTA_BaseNPC_Invoker_Forged_Spirit ||
                            creep.ClassID == ClassID.CDOTA_BaseNPC_Creep) &&
                            creep.IsAlive && creep.IsVisible && creep.IsSpawned).ToList();

            var creepW =
                ObjectManager.GetEntities<Unit>()
                    .Where(
                        creep =>
                            (creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                            creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral ||
                            creep.ClassID == ClassID.CDOTA_Unit_SpiritBear ||
                            creep.ClassID == ClassID.CDOTA_BaseNPC_Invoker_Forged_Spirit ||
                            creep.ClassID == ClassID.CDOTA_BaseNPC_Creep) &&
                            creep.IsAlive && creep.IsVisible && creep.IsSpawned).ToList();

            //Farm creeps with spells
            if (Menu.Item("qFarm").GetValue<KeyBind>().Active)
            {
                if (Utils.SleepCheck("fsleep"))
                {
                    me.Move(Game.MousePosition);
                    Utils.Sleep(66 + Game.Ping, "fsleep");
                }

                

                foreach (var creep in creepQ.Where(creep => creep.Health <=
                                                            Math.Floor((qDmg + eDmg * creep.Health) * (1 - creep.MagicDamageResist)) &&
                                                            creep.Team != me.Team).Where(creep => (me.Spellbook.SpellQ.Level > 0 && 
                                                            me.Spellbook.SpellQ.Cooldown == 0) && creep.Position.Distance2D(me.Position) <= me.Spellbook.SpellQ.GetCastRange()))
                {
                    if (soulring != null && soulring.CanBeCasted() && me.Health >= 400 && Utils.SleepCheck("soulring1"))
                    {
                        soulring.UseAbility();
                        Utils.Sleep(Game.Ping, "soulring1");
                    }
                    if (me.Spellbook.SpellQ.CanBeCasted() && Utils.SleepCheck("qfarm"))
                    {
                        me.Spellbook.SpellQ.UseAbility(creep);
                        Utils.Sleep(200 + Game.Ping, "qfarm");
                    }
                }
                if (Menu.Item("wFarm").GetValue<bool>())
                {
                    foreach (var Wcreep in creepW.Where(Wcreep => Wcreep.Health <=
                                                                Math.Floor((wDmg + eDmg * Wcreep.Health) * (1 - Wcreep.MagicDamageResist))
                                                                && Wcreep.Team != me.Team).Where(Wcreep => (me.Spellbook.SpellW.Level > 0 && me.Spellbook.SpellW.Cooldown == 0)
                                                                && Wcreep.Position.Distance2D(me.Position) <= me.Spellbook.SpellW.GetCastRange()))
                    {
                        if (soulring != null && soulring.CanBeCasted() && me.Health >= 400 &&
                            Utils.SleepCheck("soulring1"))
                        {
                            soulring.UseAbility();
                            Utils.Sleep(Game.Ping, "soulring1");
                        }
                        if (me.Spellbook.SpellW.CanBeCasted() && Utils.SleepCheck("Wfarm"))
                        {
                            me.Spellbook.SpellW.UseAbility(Wcreep);
                            Utils.Sleep(200 + Game.Ping, "Wfarm");
                        }
                    }
                }
            }

            //Combo?
            if ((Menu.Item("active").GetValue<KeyBind>().Active || Menu.Item("harass").GetValue<KeyBind>().Active) && !Menu.Item("confirmSteal").GetValue<KeyBind>().Active && me.IsAlive)
            {
                if (target != null && target.IsAlive && !target.IsInvul())
                {
                    var haslinken = target.FindItem("item_sphere");
                    var linkedsph = (haslinken != null && haslinken.Cooldown == 0) ||
                                    (target.Modifiers.Any(x => x.Name == "modifier_item_sphere_target"));
                    var targetPos = (target.Position - me.Position)*
                                    (me.Distance2D(target) - Menu.Item("saferange").GetValue<Slider>().Value)/
                                    me.Distance2D(target) + me.Position;
                    var ghostform = target.Modifiers.Any(x => x.Name == "modifier_ghost_state" ||
                                                              x.Name == "modifier_item_ethereal_blade_ethereal" ||
                                                              x.Name == "modifier_pugna_decrepify");

                    if (
                        blink != null &&
                        blink.CanBeCasted() &&
                        (me.Distance2D(target) < blink.GetCastRange() + Menu.Item("saferange").GetValue<Slider>().Value) &&
                        (me.Distance2D(target) > realWrange) &&
                        Utils.SleepCheck("blink1") && Menu.Item("blink").GetValue<bool>() &&
                        Menu.Item("active").GetValue<KeyBind>().Active
                        )
                    {
                        blink.UseAbility(targetPos);
                        Utils.Sleep(me.GetTurnTime(targetPos) + Game.Ping*2, "blink1");
                    }

                    if (soulring != null && soulring.CanBeCasted() && me.Health > me.MaximumHealth * 0.4 && Utils.SleepCheck("soulring") && me.Distance2D(target) < realWrange)
                    {
                        soulring.UseAbility();
                        Utils.Sleep(Game.Ping, "soulring");
                    }

                    if (arcane != null &&
                        (me.Mana < me.Spellbook.Spell2.ManaCost ||
                         (me.Mana < refresherComboManacost && Menu.Item("refresherToggle").GetValue<bool>() &&
                          refresher.CanBeCasted())) && arcane.CanBeCasted() && Utils.SleepCheck("arcane"))
                    {
                        arcane.UseAbility();
                        Utils.Sleep(Game.Ping, "arcane");
                    }

                    if (sheepstick != null && sheepstick.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion && !linkedsph && !target.IsHexed() && !target.IsStunned() &&
                        Utils.SleepCheck("sheepstick") && Menu.Item("active").GetValue<KeyBind>().Active && menuValue.IsEnabled(sheepstick.Name))
                    {
                        sheepstick.UseAbility(target);
                        Utils.Sleep(50+Game.Ping, "sheepstick");
                    }

                    if (orchid != null && orchid.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion && !linkedsph &&
                        !target.IsHexed() && Utils.SleepCheck("orchid") && menuValue.IsEnabled(orchid.Name))
                    {
                        orchid.UseAbility(target);
                        Utils.Sleep(50 + Game.Ping, "orchid");
                    }

                    if (bloodthorn != null && bloodthorn.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion && !linkedsph &&
                        !target.IsHexed() && Utils.SleepCheck("bloodthorn") && menuValue.IsEnabled(bloodthorn.Name))
                    {
                        bloodthorn.UseAbility(target);
                        Utils.Sleep(50 + Game.Ping, "bloodthorn");
                    }

                    if (veil != null && veil.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion &&
                        Utils.SleepCheck("veil") && menuValue.IsEnabled(veil.Name))
                    {
                        veil.UseAbility(target.Position);
                        Utils.Sleep(50+Game.Ping, "veil");
                    }

                    if (ethereal != null && ethereal.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion && !linkedsph &&
                        Utils.SleepCheck("ethereal") && menuValue.IsEnabled(ethereal.Name))
                    {
                        ethereal.UseAbility(target);
                        Utils.Sleep(50+Game.Ping, "ethereal");
                    }

                    if (halberd != null && halberd.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion &&
                        !linkedsph &&
                        Utils.SleepCheck("halberd") && Menu.Item("active").GetValue<KeyBind>().Active && menuValue.IsEnabled(halberd.Name))
                    {
                        halberd.UseAbility(target);
                        Utils.Sleep(50 + Game.Ping, "halberd");
                    }

                    if (dagon != null && dagon.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion && !linkedsph && (ethereal == null || ethereal.Cooldown < ethereal.CooldownLength-2 || ghostform) &&
                        Utils.SleepCheck("dagon") && menuValue.IsEnabled("item_dagon_5"))
                    {
                        dagon.UseAbility(target);
                        Utils.Sleep(50+Game.Ping, "dagon");
                    }

                    if (shiva != null && shiva.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion && me.Distance2D(target) < 850 &&
                        Utils.SleepCheck("shiva") && Menu.Item("active").GetValue<KeyBind>().Active && menuValue.IsEnabled(shiva.Name))
                    {
                        shiva.UseAbility();
                        Utils.Sleep(50+Game.Ping, "shiva");
                    }
                    
                    if (me.Spellbook.SpellQ != null && me.Spellbook.SpellQ.CanBeCasted() &&
                        me.Mana > me.Spellbook.Spell1.ManaCost && !target.IsMagicImmune() && !target.IsIllusion && me.CanCast() &&
                        Utils.SleepCheck("Q") && (!me.Spellbook.Spell2.CanBeCasted() || linkedsph) && (ethereal == null || ethereal.Cooldown < ethereal.CooldownLength - 1.5 || ghostform))
                    {
                        me.Spellbook.SpellQ.UseAbility(target);
                        Utils.Sleep(200 + Game.Ping, "Q");
                    }

                    if (me.Spellbook.Spell2 != null && (me.Distance2D(target) < me.Spellbook.SpellW.GetCastRange()) &&
                        me.Spellbook.Spell2.CanBeCasted() && me.Mana > me.Spellbook.Spell2.ManaCost && !linkedsph && me.CanCast() &&
                        !target.IsMagicImmune() && !target.IsIllusion && Utils.SleepCheck("W") && (ethereal == null || ethereal.Cooldown < ethereal.CooldownLength - 1.5 || ghostform || target.IsChanneling()))
                    {
                        me.Spellbook.Spell2.UseAbility(target);
                        Utils.Sleep(400 + Game.Ping, "W");
                    }

                    if (me.Spellbook.Spell2 != null &&
                        (me.Distance2D(target) < realWrange) &&
                        (me.Distance2D(target) > me.Spellbook.SpellW.GetCastRange()) && me.Spellbook.Spell2.CanBeCasted() && me.CanCast() &&
                        me.Mana > me.Spellbook.Spell2.ManaCost && !target.IsMagicImmune() && !target.IsIllusion && !linkedsph &&
                        Utils.SleepCheck("W") && (ethereal == null || ethereal.Cooldown < ethereal.CooldownLength-1.5 || ghostform || target.IsChanneling()))
                    {
                        me.Spellbook.Spell2.CastSkillShot(target);
                        Utils.Sleep(400 + Game.Ping, "W");
                    }

                    if (
                        (!(me.Spellbook.Spell2.CanBeCasted() && me.Spellbook.Spell1.CanBeCasted()) || target.IsMagicImmune() || !me.CanCast()) && !ghostform &&
                        me.CanAttack() && me.Distance2D(target) < 350+me.HullRadius+target.HullRadius &&
                        target != null)
                    {
                        Orbwalking.Orbwalk(target);
                    }
                    else if (me.CanMove() && !me.IsChanneling() && Utils.SleepCheck("movesleep") && (me.Distance2D(target) >= 350 + me.HullRadius + target.HullRadius || ghostform || !me.CanAttack()))
                    {
                        me.Move(Game.MousePosition);
                        Utils.Sleep(50 + Game.Ping, "movesleep");
                    }

                    if (Menu.Item("refresherToggle").GetValue<bool>() && !target.IsMagicImmune() && refresher != null &&
                        refresher.CanBeCasted() && me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").CanBeCasted() && (ethereal == null || ethereal.Cooldown < ethereal.CooldownLength-2 || ghostform) &&
                        Utils.SleepCheck("ultiRefresher") && Menu.Item("active").GetValue<KeyBind>().Active)
                    {
                        me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").UseAbility();
                        Utils.Sleep(100 + Game.Ping, "ultiRefresher");
                    }

                    if (Menu.Item("refresherToggle").GetValue<bool>() && refresher != null && refresher.CanBeCasted() && me.Mana > refresherComboManacost &&
                        Utils.SleepCheck("refresher") && !target.IsMagicImmune() && target != null &&
                        !me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").CanBeCasted() && !me.Spellbook.Spell2.CanBeCasted() &&
                        (orchid == null || orchid.Cooldown > 0) &&
                        (bloodthorn == null || bloodthorn.Cooldown > 0) &&
                        (sheepstick == null || sheepstick.Cooldown > 0) &&
                        (veil == null || veil.Cooldown > 0) &&
                        (shiva == null || shiva.Cooldown > 0) &&
                        (halberd == null || halberd.Cooldown > 0) &&
                        (dagon == null || dagon.Cooldown > 0) &&
                        (ethereal == null || ethereal.Cooldown > 0) && 
                        Menu.Item("active").GetValue<KeyBind>().Active)
                    {
                        refresher.UseAbility();
                        Utils.Sleep(300 + Game.Ping, "refresher");
                    }
                }
                else if (!me.IsChanneling() && Utils.SleepCheck("movesleep"))
                {
                    me.Move(Game.MousePosition);
                    Utils.Sleep(50 + Game.Ping, "movesleep");
                }
            }
        }

        public static void cancelult()
        {
            me = ObjectManager.LocalHero;
            float eDmg, rDmg;

            eDmg = me.Spellbook.SpellE.AbilitySpecialData.FirstOrDefault(x => x.Name == "damage_health_pct").GetValue(me.Spellbook.SpellE.Level - 1) / 100 * GetMyTotalSpellAmp() * vhero.Health;
            rDmg = me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").AbilitySpecialData.FirstOrDefault(x => x.Name == "damage").GetValue(me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").Level - 1);

            var rDamage = vhero.SpellDamageTaken(rDmg, DamageType.Magical, me, me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").Name);
            var eDamage = eDmg * (1 - vhero.MagicDamageResist);

            //Console.WriteLine("rDamage = " + rDamage);
            //Console.WriteLine("eDamage = " + eDamage);

            if (Menu.Item("stealEdmg").GetValue<bool>() && me.Distance2D(vhero) < me.Spellbook.SpellE.GetCastRange())
                rDamage = rDamage + eDamage;
           
            if (vhero.NetworkName == "CDOTA_Unit_Hero_SkeletonKing" &&
                vhero.Spellbook.SpellR.CanBeCasted())
                rDamage = 0;

            if (vhero.NetworkName == "CDOTA_Unit_Hero_Tusk" &&
                vhero.Spellbook.SpellW.CooldownLength - 3 > vhero.Spellbook.SpellQ.Cooldown)
                rDamage = 0;

            var unkillabletarget1 = vhero.Modifiers.Any(
                x =>    x.Name == "modifier_abaddon_borrowed_time" || 
                        x.Name == "modifier_dazzle_shallow_grave" ||
                        x.Name == "modifier_obsidian_destroyer_astral_imprisonment_prison" ||
                        x.Name == "modifier_puck_phase_shift" ||
                        x.Name == "modifier_brewmaster_storm_cyclone" || 
                        x.Name == "modifier_eul_cyclone" ||
                        x.Name == "modifier_item_aegis" || 
                        x.Name == "modifier_slark_shadow_dance" ||
                        x.Name == "modifier_ember_spirit_flame_guard" ||
                        x.Name == "modifier_abaddon_aphotic_shield" || 
                        x.Name == "modifier_phantom_lancer_doppelwalk_phase" ||
                        x.Name == "modifier_shadow_demon_disruption" || 
                        x.Name == "modifier_nyx_assassin_spiked_carapace" ||
                        x.Name == "modifier_templar_assassin_refraction_absorb" || 
                        x.Name == "modifier_necrolyte_reapers_scythe" ||
                        x.Name == "modifier_storm_spirit_ball_lightning" || 
                        x.Name == "modifier_ember_spirit_sleight_of_fist_caster_invulnerability" ||
                        x.Name == "modifier_ember_spirit_fire_remnant" || 
                        x.Name == "modifier_snowball_movement" || 
                        x.Name == "modifier_snowball_movement_friendly");

            if (vhero.Health > rDamage || !vhero.IsAlive || vhero.IsIllusion || unkillabletarget1 || vhero.IsMagicImmune() || (vhero.Name == "npc_dota_hero_slark" && !vhero.IsVisible)) me.Stop();
            vhero = null;
        }

        public static void Killsteal(EventArgs args)
        {
            if (!menuadded) return;
            me = ObjectManager.LocalHero;

            float eDmg, rDmg;

            rDmg = me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").AbilitySpecialData.FirstOrDefault(x => x.Name == "damage").GetValue(me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").Level - 1);

            if (Utils.SleepCheck("killstealR") && Game.IsInGame && me != null &&
                me.ClassID == ClassID.CDOTA_Unit_Hero_Zuus && me.IsAlive)
            {
                drawStealNotice = false;
                
                if (
                    ((!Menu.Item("active").GetValue<KeyBind>().Active && Menu.Item("useRincombo").GetValue<bool>()) ||
                     !Menu.Item("useRincombo").GetValue<bool>() ||
                     !Menu.Item("stealToggle").GetValue<KeyBind>().Active) &&
                     me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").Level > 0 && me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").Cooldown == 0
                    )
                {
                    var enemy =
                        ObjectManager.GetEntities<Hero>()
                            .Where(
                                e =>
                                    e.Team != me.Team && e.IsAlive && e.IsVisible && !e.IsIllusion &&
                                    !e.UnitState.HasFlag(UnitState.MagicImmune) &&
                                    e.ClassID != ClassID.CDOTA_Unit_Hero_Beastmaster_Hawk &&
                                    e.ClassID != ClassID.CDOTA_Unit_Hero_Beastmaster_Boar &&
                                    e.ClassID != ClassID.CDOTA_Unit_Hero_Beastmaster_Beasts &&
                                    e.ClassID != ClassID.CDOTA_Unit_Brewmaster_PrimalEarth &&
                                    e.ClassID != ClassID.CDOTA_Unit_Brewmaster_PrimalFire &&
                                    e.ClassID != ClassID.CDOTA_Unit_Brewmaster_PrimalStorm &&
                                    e.ClassID != ClassID.CDOTA_Unit_Undying_Tombstone &&
                                    e.ClassID != ClassID.CDOTA_Unit_Undying_Zombie &&
                                    e.ClassID != ClassID.CDOTA_Ability_Juggernaut_HealingWard).ToList();

                    foreach (var v in enemy)
                    {
                        eDmg = me.Spellbook.SpellE.AbilitySpecialData.FirstOrDefault(x => x.Name == "damage_health_pct").GetValue(me.Spellbook.SpellE.Level - 1) / 100 * GetMyTotalSpellAmp() * v.Health;

                        var rDamage = v.SpellDamageTaken(rDmg, DamageType.Magical, me, me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").Name);
                        var eDamage = eDmg * (1 - v.MagicDamageResist);

                        //Console.WriteLine("rDamage = " + v.SpellDamageTaken(rDmg, DamageType.Magical, me, me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").Name));
                        //Console.WriteLine("eDamage = " + eDamage);

                        if (Menu.Item("stealEdmg").GetValue<bool>() && me.Distance2D(v) < me.Spellbook.SpellE.GetCastRange())
                            rDamage = rDamage + eDamage;

                        if (v.NetworkName == "CDOTA_Unit_Hero_SkeletonKing" &&
                            v.Spellbook.SpellR.CanBeCasted())
                            rDamage = 0;
                        if (v.NetworkName == "CDOTA_Unit_Hero_Tusk" &&
                            v.Spellbook.SpellW.CooldownLength - 3 > v.Spellbook.SpellQ.Cooldown)
                            rDamage = 0;

                        var unkillabletarget = v.Modifiers.Any(
                        x =>    x.Name == "modifier_abaddon_borrowed_time" || 
                                x.Name == "modifier_dazzle_shallow_grave" ||
                                x.Name == "modifier_obsidian_destroyer_astral_imprisonment_prison" || 
                                x.Name == "modifier_puck_phase_shift" ||
                                x.Name == "modifier_brewmaster_storm_cyclone" || 
                                x.Name == "modifier_eul_cyclone" ||
                                x.Name == "modifier_item_aegis" || 
                                x.Name == "modifier_slark_shadow_dance" || 
                                x.Name == "modifier_ember_spirit_flame_guard" ||
                                x.Name == "modifier_abaddon_aphotic_shield" || 
                                x.Name == "modifier_phantom_lancer_doppelwalk_phase" ||
                                x.Name == "modifier_shadow_demon_disruption" || 
                                x.Name == "modifier_nyx_assassin_spiked_carapace" || 
                                x.Name == "modifier_templar_assassin_refraction_absorb" || 
                                x.Name == "modifier_necrolyte_reapers_scythe" ||
                                x.Name == "modifier_storm_spirit_ball_lightning" || 
                                x.Name == "modifier_ember_spirit_sleight_of_fist_caster_invulnerability" ||
                                x.Name == "modifier_ember_spirit_fire_remnant" || 
                                x.Name == "modifier_snowball_movement" || 
                                x.Name == "modifier_snowball_movement_friendly");

                        if (v.Health < rDamage && v != null && !v.IsIllusion && !unkillabletarget && (!v.IsInvisible() || (v.IsInvisible() && v.IsVisible)))
                        {
                            drawStealNotice = true;

                            steallableHero = v.NetworkName.Replace("CDOTA_Unit_Hero_", "").ToUpper();

                            if (
                                (Menu.Item("confirmSteal").GetValue<KeyBind>().Active ||
                                 Menu.Item("stealToggle").GetValue<KeyBind>().Active) && !v.IsIllusion)
                            {
                                if (soulring != null && soulring.CanBeCasted() && Utils.SleepCheck("soulring") &&
                                    me.Mana < me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").ManaCost &&
                                    me.Mana + 150 > me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").ManaCost)
                                {
                                    soulring.UseAbility();
                                    Utils.Sleep(Game.Ping, "soulring");
                                }
                                if (arcane != null && arcane.CanBeCasted() && Utils.SleepCheck("arcane") &&
                                    me.Mana < me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").ManaCost &&
                                    me.Mana + 135 > me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").ManaCost)
                                {
                                    arcane.UseAbility();
                                    Utils.Sleep(Game.Ping, "arcane");
                                }
                                if (arcane != null && soulring != null && Utils.SleepCheck("arcane") &&
                                    arcane.CanBeCasted() && soulring.CanBeCasted() &&
                                    me.Mana < me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").ManaCost &&
                                    me.Mana + 285 > me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").ManaCost)
                                {
                                    arcane.UseAbility();
                                    soulring.UseAbility();
                                    Utils.Sleep(Game.Ping, "arcane");
                                }
                                if (me.Mana > me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").ManaCost)
                                {
                                    if (soulring != null && soulring.CanBeCasted() && Utils.SleepCheck("soulring") && me.Health > me.MaximumHealth * 0.4)
                                    {
                                        soulring.UseAbility();
                                        Utils.Sleep(Game.Ping, "soulring");
                                    }
                                    me.Spellbook.Spells.FirstOrDefault(x => x.Name == "zuus_thundergods_wrath").UseAbility();
                                    vhero = v;
                                    DelayAction.Add(385, cancelult);
                                    Utils.Sleep(400, "killstealR");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            _text.Dispose();
            _notice.Dispose();
            _line.Dispose();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            me = ObjectManager.LocalHero;
            if (!menuadded) return;
            ParticleEffect scope;

            #region target draw

            if (target != null && target.IsAlive && !target.IsInvul())
            {
                if (Menu.Item("drawtargetglow").GetValue<bool>())
                    for (var i = 50; i < 52; i++)
                    {
                        if (Effect.TryGetValue(i, out scope)) continue;
                        heronametargeted = target.NetworkName;
                        heronametargeted = heronametargeted.Replace("CDOTA_Unit_Hero_", "");
                        scope =
                            target.AddParticleEffect(
                                @"particles\units\heroes\hero_beastmaster\beastmaster_wildaxe_glow.vpcf");
                        scope.SetControlPoint(1, new Vector3(200, 0, 0));
                        Effect.Add(i, scope);
                    }
            }
            if (target == null || !target.IsAlive || target.IsInvul() ||
                !Menu.Item("drawtargetglow").GetValue<bool>() ||
                target.NetworkName.Replace("CDOTA_Unit_Hero_", "") != heronametargeted)
            {
                for (var i = 50; i < 52; i++)
                {
                    if (!Effect.TryGetValue(i, out scope)) continue;
                    scope.Dispose();
                    Effect.Remove(i);
                }
            }

            #endregion

            #region blink ready glow

            if (Menu.Item("drawblinkready").GetValue<bool>())
            {
                if (blink != null && blink.Cooldown == 0)
                {
                    for (var l = 30; l < 34; l++)
                    {
                        if (!Effect.TryGetValue(l, out effect))
                        {
                            effect =
                                me.AddParticleEffect(
                                    @"particles\econ\courier\courier_baekho\courier_baekho_ambient_glow.vpcf");
                            effect.SetControlPoint(1, new Vector3(200, 0, 0));
                            Effect.Add(l, effect);
                        }
                    }
                }

                if (blink == null || (blink != null && blink.Cooldown > 0))
                {
                    for (var l = 30; l < 34; l++)
                    {
                        if (Effect.TryGetValue(l, out effect))
                        {
                            effect.Dispose();
                            Effect.Remove(l);
                        }
                    }
                }
            }
            else
                for (var l = 30; l < 34; l++)
                {
                    if (Effect.TryGetValue(l, out effect))
                    {
                        effect.Dispose();
                        Effect.Remove(l);
                    }
                }

            #endregion

            if (realWrange != Wdrawn)
            {
                Wdrawn = realWrange;
                if (Effect.TryGetValue(1, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(1);
                }
                if (!Effect.TryGetValue(1, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    effect.SetControlPoint(1, new Vector3(realWrange, 0, 0));
                    Effect.Add(1, effect);
                }
            }

            if (Menu.Item("drawWrange").GetValue<bool>())
            {
                if (!Effect.TryGetValue(1, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    Wdrawn = realWrange;
                    effect.SetControlPoint(1, new Vector3(realWrange, 0, 0));
                    Effect.Add(1, effect);
                }
            }
            else
            {
                if (Effect.TryGetValue(1, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(1);
                }
            }

            if (me.Spellbook.SpellQ.GetCastRange() != Qdrawn)
            {
                Qdrawn = me.Spellbook.SpellQ.GetCastRange();
                if (Effect.TryGetValue(3, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(3);
                }
                if (!Effect.TryGetValue(3, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    effect.SetControlPoint(1, new Vector3(me.Spellbook.SpellQ.GetCastRange(), 0, 0));
                    Effect.Add(3, effect);
                }
            }

            if (Menu.Item("drawQrange").GetValue<bool>())
            {
                if (!Effect.TryGetValue(3, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    effect.SetControlPoint(1, new Vector3(me.Spellbook.SpellQ.GetCastRange(), 0, 0));
                    Effect.Add(3, effect);
                }
            }
            else
            {
                if (Effect.TryGetValue(3, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(3);
                }
            }

            if (Menu.Item("saferange").GetValue<Slider>().Value + blink.GetCastRange() != blinkdrawnr && blink != null)
            {
                blinkdrawnr = Menu.Item("saferange").GetValue<Slider>().Value + (int)blink.GetCastRange();
                if (Effect.TryGetValue(2, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(2);
                }
                if (!Effect.TryGetValue(2, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    effect.SetControlPoint(1,
                        new Vector3(blinkdrawnr, 0, 0));
                    Effect.Add(2, effect);
                }
            }

            if (Menu.Item("drawblinkrange").GetValue<bool>() && blink != null)
            {
                if (!Effect.TryGetValue(2, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    blinkdrawnr = Menu.Item("saferange").GetValue<Slider>().Value + (int)blink.GetCastRange();
                    effect.SetControlPoint(1,
                        new Vector3(blinkdrawnr, 0, 0));
                    Effect.Add(2, effect);
                }
            }
            else
            {
                if (Effect.TryGetValue(2, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(2);
                }
            }
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (!menuadded) return;
            if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame)
                return;

            var player = ObjectManager.LocalPlayer;
            me = ObjectManager.LocalHero;
            if (player == null || player.Team == Team.Observer || me.ClassID != ClassID.CDOTA_Unit_Hero_Zuus)
                return;

            if (Menu.Item("active").GetValue<KeyBind>().Active)
            {
                DrawBox(2, 37, 110, 20, 1, new ColorBGRA(0, 200, 100, 100));
                DrawFilledBox(2, 37, 110, 20, new ColorBGRA(0, 0, 0, 100));
                DrawShadowText("Zeus#: Comboing!", 4, 37, Color.LightBlue, _text);
            }
            if (Game.IsKeyDown(Menu.Item("stealToggle").GetValue<KeyBind>().Key) || statechanged)
            {
                if (Menu.Item("stealToggle").GetValue<KeyBind>().Active)
                {
                    statechanged = true;
                    DrawBox(114, 37, 100, 20, 1, new ColorBGRA(0, 200, 100, 100));
                    DrawFilledBox(114, 37, 100, 20, new ColorBGRA(0, 0, 0, 100));
                    DrawShadowText("Auto Steal Mode", 114, 37, Color.LightBlue, _text);
                    if (Utils.SleepCheck("once"))
                    {
                        DelayAction.Add(5000, stateswitch);
                        Utils.Sleep(5000, "once");
                    }
                }
                else
                {
                    statechanged = true;
                    DrawBox(114, 37, 115, 20, 1, new ColorBGRA(0, 200, 100, 100));
                    DrawFilledBox(114, 37, 115, 20, new ColorBGRA(0, 0, 0, 100));
                    DrawShadowText("Manual Steal Mode", 114, 37, Color.LightBlue, _text);
                    if (Utils.SleepCheck("once"))
                    {
                        DelayAction.Add(5000, stateswitch);
                        Utils.Sleep(5000, "once");
                    }
                }
            }

            if (drawStealNotice && !Menu.Item("confirmSteal").GetValue<KeyBind>().Active &&
                !Menu.Item("stealToggle").GetValue<KeyBind>().Active)
            {
                DrawShadowText(
                    "PRESS [" + Utils.KeyToText(Menu.Item("confirmSteal").GetValue<KeyBind>().Key) + "] FOR STEAL " + steallableHero +
                    "!", 7, 400, Color.Yellow, _notice);
            }
        }

        private static void stateswitch()
        {
            statechanged = false;
        }
        private static void Drawing_OnPostReset(EventArgs args)
        {
            _text.OnResetDevice();
            _notice.OnResetDevice();
            _line.OnResetDevice();
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
            _text.OnLostDevice();
            _notice.OnLostDevice();
            _line.OnLostDevice();
        }

        public static void DrawFilledBox(float x, float y, float w, float h, Color color)
        {
            var vLine = new Vector2[2];

            _line.GLLines = true;
            _line.Antialias = false;
            _line.Width = w;

            vLine[0].X = x + w/2;
            vLine[0].Y = y;
            vLine[1].X = x + w/2;
            vLine[1].Y = y + h;

            _line.Begin();
            _line.Draw(vLine, color);
            _line.End();
        }

        public static void DrawBox(float x, float y, float w, float h, float px, Color color)
        {
            DrawFilledBox(x, y + h, w, px, color);
            DrawFilledBox(x - px, y, px, h, color);
            DrawFilledBox(x, y - px, w, px, color);
            DrawFilledBox(x + w, y, px, h, color);
        }

        public static void DrawShadowText(string stext, int x, int y, Color color, Font f)
        {
            f.DrawText(null, stext, x + 1, y + 1, Color.Black);
            f.DrawText(null, stext, x, y, color);
        }

        public static float GetMyTotalSpellAmp ()
        {
            me = ObjectManager.LocalHero;
            var totalSpellAmp = 0f;

            var talent = me.Spellbook.Spells.FirstOrDefault(x => x.Name.Contains("special_bonus_spell_amplify"));
            if (talent?.Level > 0)
            {
                totalSpellAmp += talent.GetAbilityData("value") / 100f;
            }

                    if (me.Inventory.Items.FirstOrDefault(x => x.ClassID == ClassID.CDOTA_Item_Aether_Lens) != null)
                    {
                        totalSpellAmp += me.Inventory.Items.FirstOrDefault(x => x.ClassID == ClassID.CDOTA_Item_Aether_Lens).GetAbilityData("spell_amp") / 100f;
                    }

            if (me != null)
            {
                totalSpellAmp += (100f + me.TotalIntelligence / 16f) / 100f;
            }

            return totalSpellAmp;
        }
    }
}


