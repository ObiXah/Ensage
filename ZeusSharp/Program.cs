﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private static Item orchid, sheepstick, veil, soulring, arcane, blink, shiva, dagon, refresher, ethereal;
        private static bool drawStealNotice;
        private static bool menuadded;
        private static readonly int manaForQ = 235;
        private static int Wdrawn;
        private static int blinkdrawnr;
        private static Font _text;
        private static Font _notice;
        private static Line _line;
        private static string steallableHero;
        private static string heronametargeted;
        private static Hero target;
        private static Hero me;
        private static readonly Dictionary<int, ParticleEffect> Effect = new Dictionary<int, ParticleEffect>();
        private static readonly Menu Menu = new Menu("Zeus#", "Zeus#", true, "npc_dota_hero_zuus", true);
        private static int[] rDmg = new int[3] { 225, 350, 475 };
        private static readonly int[] qDmg = new int[4] {85, 100, 115, 145};
        private static readonly int[] eDmg = new int[5] {0, 5, 7, 9, 11};

        private static void Main()
        {
            Game.OnUpdate += Killsteal;
            Game.OnUpdate += Game_OnUpdate;
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


            var comboMenu = new Menu("Combo Tweaks", "combomenu", false, @"..\other\statpop_exclaim", true);
            comboMenu.AddItem(
                new MenuItem("blink", "Use Blink").SetValue(true)
                    .SetTooltip("Blinks to target but not closer than specified range."));
            comboMenu.AddItem(
                new MenuItem("refresherToggle", "Refresher Use").SetValue(false)
                    .SetTooltip("Auto use refresher for 2x ultimate."));
            comboMenu.AddItem(
                new MenuItem("dforceAA", "Force Auto-Attacks").SetValue(false)
                    .SetTooltip("Will go for autoattacks even when out of range."));
            comboMenu.AddItem(
                new MenuItem("targetsearchrange", "Target Search Range").SetValue(new Slider(1000, 128, 2500))
                    .SetTooltip("Radius of target search range around cursor."));
            comboMenu.AddItem(
                new MenuItem("saferange", "Blink not closer than").SetValue(new Slider(650, 125, 850))
                    .SetTooltip(
                        "Increases combo range with blink. P.S. No point in blinking in melee to da face. Shoutout to Evervolv1337 ;)"));
            comboMenu.AddItem(
                new MenuItem("Wrealrange", "W Non-target Range").SetValue(new Slider(970, 700, 1075))
                    .SetTooltip("Try to W ground close to enemy giving 1075 max range. Reduce range in case of misses."));

            var stealMenu = new Menu("Ultimate Usage", "stealmenu", false, "zuus_thundergods_wrath", true);
            stealMenu.AddItem(new MenuItem("stealToggle", "Auto Steal").SetValue(true).SetTooltip("Auto R on killable."));
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
                new MenuItem("qFarm", "Farm Key").SetValue(new KeyBind('F', KeyBindType.Press))
                    .SetTooltip("Hold this key to farm with Q."));

            Menu.AddSubMenu(comboMenu);
            Menu.AddSubMenu(stealMenu);
            Menu.AddSubMenu(drawMenu);

            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Drawing.OnDraw += Drawing_OnDraw;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            me = ObjectMgr.LocalHero;
            if (!Game.IsInGame || me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Zuus)
            {
                if (menuadded)
                {
                    menuadded = false;
                    Menu.RemoveFromMainMenu();
                }
                return;
            }
            if ((Game.IsInGame || me != null || me.ClassID == ClassID.CDOTA_Unit_Hero_Zuus) && !menuadded)
            {
                menuadded = true;
                Menu.AddToMainMenu();
            }
            target = me.ClosestToMouseTarget(Menu.Item("targetsearchrange").GetValue<Slider>().Value);

            // Items
            orchid = me.FindItem("item_orchid");
            sheepstick = me.FindItem("item_sheepstick");
            veil = me.FindItem("item_veil_of_discord");
            soulring = me.FindItem("item_soul_ring");
            arcane = me.FindItem("item_arcane_boots");
            blink = me.FindItem("item_blink");
            shiva = me.FindItem("item_shivas_guard");
            dagon = me.Inventory.Items.FirstOrDefault(item => item.Name.Contains("item_dagon"));
            refresher = me.FindItem("item_refresher");
            ethereal = me.FindItem("item_ethereal_blade");

            var refresherComboManacost = me.Spellbook.Spell4.ManaCost + me.Spellbook.Spell2.ManaCost +
                                         me.Spellbook.Spell1.ManaCost;

            // Manacost calculation
            if (veil != null)
                refresherComboManacost += veil.ManaCost;

            if (orchid != null)
                refresherComboManacost += orchid.ManaCost;

            if (sheepstick != null)
                refresherComboManacost += sheepstick.ManaCost;

            if (refresher != null)
                refresherComboManacost += refresher.ManaCost;
            var qlvl = me.Spellbook.SpellQ.Level - 1;
            var elvl = me.Spellbook.SpellE.Level;

            var creepQ =
                ObjectMgr.GetEntities<Creep>()
                    .Where(
                        creep =>
                            (creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane ||
                             creep.ClassID != ClassID.CDOTA_BaseNPC_Creep_Siege ||
                             creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral ||
                             creep.ClassID == ClassID.CDOTA_Unit_SpiritBear ||
                             creep.ClassID == ClassID.CDOTA_BaseNPC_Invoker_Forged_Spirit ||
                             creep.ClassID == ClassID.CDOTA_BaseNPC_Creep) &&
                            creep.IsAlive && creep.IsVisible && creep.IsSpawned).ToList();

            if (Menu.Item("qFarm").GetValue<KeyBind>().Active)
            {
                if (Utils.SleepCheck("fsleep"))
                {
                    me.Move(Game.MousePosition);
                    Utils.Sleep(66 + Game.Ping, "fsleep");
                }
                foreach (var creep in creepQ.Where(creep => me.Spellbook.SpellQ.CanBeCasted() &&
                                                            creep.Health <=
                                                            Math.Floor((qDmg[qlvl] + eDmg[elvl]*0.01*creep.Health)*(1 - creep.MagicDamageResist)) &&
                                                            creep.Team != me.Team).Where(creep => me.Spellbook.SpellQ.CanBeCasted() && creep.Position.Distance2D(me.Position) <= 850 &&
                                                                                                  Utils.SleepCheck("qfarm")))
                {
                    if (soulring != null && soulring.CanBeCasted() && me.Health >= 400)
                    {
                        soulring.UseAbility();
                    }
                    else
                        me.Spellbook.SpellQ.UseAbility(creep);
                    Utils.Sleep(100 + Game.Ping, "qfarm");
                }
            }

            if (Menu.Item("active").GetValue<KeyBind>().Active && !Menu.Item("confirmSteal").GetValue<KeyBind>().Active &&
                me.CanCast() && me.IsAlive)
            {
                if (target != null && target.IsAlive && !target.IsInvul())
                {
                    var targetPos = (target.Position - me.Position)*
                                    (me.Distance2D(target) - Menu.Item("saferange").GetValue<Slider>().Value)/
                                    me.Distance2D(target) + me.Position;
                    if (
                        blink != null &&
                        blink.CanBeCasted() &&
                        (me.Distance2D(target) < 1200 + Menu.Item("saferange").GetValue<Slider>().Value) &&
                        (me.Distance2D(target) > Menu.Item("saferange").GetValue<Slider>().Value + 125) &&
                        Utils.SleepCheck("blink1") && Menu.Item("blink").GetValue<bool>()
                        )
                    {
                        blink.UseAbility(targetPos);
                        Utils.Sleep(Game.Ping, "blink1");
                    }

                    Utils.Sleep(me.GetTurnTime(target), "blink");

                    if (soulring != null && me.Health > 300 &&
                        (me.Mana < me.Spellbook.Spell2.ManaCost ||
                         (me.Mana < refresherComboManacost && Menu.Item("refresherToggle").GetValue<bool>() &&
                          refresher.CanBeCasted())) && soulring.CanBeCasted() && Utils.SleepCheck("soulring"))
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

                    if (sheepstick != null && sheepstick.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion &&
                        Utils.SleepCheck("sheepstick"))
                    {
                        sheepstick.UseAbility(target);
                        Utils.Sleep(Game.Ping, "sheepstick");
                    }

                    if (orchid != null && orchid.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion &&
                        !target.IsHexed() && Utils.SleepCheck("orchid"))
                    {
                        orchid.UseAbility(target);
                        Utils.Sleep(Game.Ping, "orchid");
                    }

                    if (veil != null && veil.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion &&
                        Utils.SleepCheck("veil"))
                    {
                        veil.UseAbility(target.Position);
                        Utils.Sleep(Game.Ping, "veil");
                    }

                    if (ethereal != null && ethereal.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion &&
                        Utils.SleepCheck("ethereal"))
                    {
                        ethereal.UseAbility(target);
                        Utils.Sleep(Game.Ping, "ethereal");
                    }

                    Utils.ChainStun(me, 100, null, false);

                    if (dagon != null && dagon.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion &&
                        Utils.SleepCheck("dagon"))
                    {
                        dagon.UseAbility(target);
                        Utils.Sleep(Game.Ping, "dagon");
                    }

                    if (shiva != null && shiva.CanBeCasted() && !target.IsMagicImmune() && !target.IsIllusion &&
                        Utils.SleepCheck("shiva"))
                    {
                        shiva.UseAbility();
                        Utils.Sleep(Game.Ping, "shiva");
                    }

                    if (me.Spellbook.SpellQ != null && me.Spellbook.SpellQ.CanBeCasted() &&
                        me.Mana > me.Spellbook.Spell1.ManaCost && !target.IsMagicImmune() && !target.IsIllusion &&
                        Utils.SleepCheck("Q") &&
                        (!me.Spellbook.Spell2.CanBeCasted() ||
                         me.Distance2D(target) > Menu.Item("Wrealrange").GetValue<Slider>().Value) && me.Mana > manaForQ)
                    {
                        me.Spellbook.SpellQ.UseAbility(target);
                        Utils.Sleep(150 + Game.Ping, "Q");
                    }

                    if (me.Spellbook.Spell2 != null && (me.Distance2D(target) < 700) &&
                        me.Spellbook.Spell2.CanBeCasted() && me.Mana > me.Spellbook.Spell2.ManaCost &&
                        !target.IsMagicImmune() && !target.IsIllusion && Utils.SleepCheck("W"))
                    {
                        me.Spellbook.Spell2.UseAbility(target);
                        Utils.Sleep(200 + Game.Ping, "W");
                    }

                    if (me.Spellbook.Spell2 != null &&
                        (me.Distance2D(target) < Menu.Item("Wrealrange").GetValue<Slider>().Value) &&
                        (me.Distance2D(target) > 700) && me.Spellbook.Spell2.CanBeCasted() &&
                        me.Mana > me.Spellbook.Spell2.ManaCost && !target.IsMagicImmune() && !target.IsIllusion &&
                        Utils.SleepCheck("Wnontarget"))
                    {
                        var wPos = (target.Position - me.Position)*
                                   (me.Distance2D(target) - (Menu.Item("Wrealrange").GetValue<Slider>().Value - 700))/
                                   me.Distance2D(target) + me.Position;
                        me.Spellbook.Spell2.UseAbility(wPos);
                        Utils.Sleep(70 + Game.Ping, "Wnontarget");
                    }

                    if (
                        (
                            !(me.Spellbook.Spell2.CanBeCasted() && me.Spellbook.Spell1.CanBeCasted()) ||
                            target.IsMagicImmune() || me.IsSilenced()
                            ) &&
                        me.CanAttack() &&
                        (Menu.Item("dforceAA").GetValue<bool>() || me.Distance2D(target) < 350) &&
                        target != null &&
                        Utils.SleepCheck("attack")
                        )
                    {
                        me.Attack(target);
                        Utils.Sleep(50 + Game.Ping, "attack");
                    }
                    else if (me.CanMove() && !me.IsChanneling() && Utils.SleepCheck("movesleep") &&
                             !Menu.Item("dforceAA").GetValue<bool>() && me.Distance2D(target) > 350)
                    {
                        me.Move(Game.MousePosition);
                        Utils.Sleep(50 + Game.Ping, "movesleep");
                    }
                    if (Menu.Item("refresherToggle").GetValue<bool>() && !target.IsMagicImmune() && refresher != null &&
                        refresher.CanBeCasted() && me.Spellbook.Spell4.CanBeCasted() &&
                        Utils.SleepCheck("ultiRefresher"))
                    {
                        me.Spellbook.Spell4.UseAbility();
                        Utils.Sleep(100 + Game.Ping, "ultiRefresher");
                    }

                    if (Menu.Item("refresherToggle").GetValue<bool>() && refresher != null && refresher.CanBeCasted() &&
                        Utils.SleepCheck("refresher") && !target.IsMagicImmune() && target != null &&
                        !me.Spellbook.Spell4.CanBeCasted() && !me.Spellbook.Spell2.CanBeCasted())
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

        public static void Killsteal(EventArgs args)
        {
            me = ObjectMgr.LocalHero;

            if (Utils.SleepCheck("killstealR") && Game.IsInGame && me != null &&
                me.ClassID == ClassID.CDOTA_Unit_Hero_Zuus)
            {
                drawStealNotice = false;

                if (me.HasItem(ClassID.CDOTA_Item_UltimateScepter))
                {
                    rDmg = new int[3] {440, 540, 640};
                }
                else
                {
                    rDmg = new int[3] {225, 350, 475};
                }

                if (
                    ((!Menu.Item("active").GetValue<KeyBind>().Active && Menu.Item("useRincombo").GetValue<bool>()) ||
                     !Menu.Item("useRincombo").GetValue<bool>() ||
                     !Menu.Item("stealToggle").GetValue<bool>()) &&
                    me.Spellbook.Spell4.Cooldown == 0 && me.Spellbook.Spell4.Level > 0
                    )
                {
                    var enemy =
                        ObjectMgr.GetEntities<Hero>()
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
                        var damage = Math.Floor(rDmg[me.Spellbook.Spell4.Level - 1]*(1 - v.MagicDamageResist));
                        if (Menu.Item("stealEdmg").GetValue<bool>() && me.Distance2D(v) < 1200)
                            damage = damage + eDmg[me.Spellbook.Spell3.Level]*0.01*v.Health;
                        
                        var unkillabletarget = v.Modifiers.Any(
                            x => x.Name == "modifier_abaddon_borrowed_time" || x.Name == "modifier_dazzle_shallow_grave" ||
                                 x.Name == "modifier_obsidian_destroyer_astral_imprisonment_prison");
                        
                        if (v.Health < damage - v.Level && v != null && !v.IsIllusion && !unkillabletarget)
                        {
                            drawStealNotice = true;

                            steallableHero = v.NetworkName.Replace("CDOTA_Unit_Hero_", "").ToUpper();

                            if (
                                (Menu.Item("confirmSteal").GetValue<KeyBind>().Active ||
                                 Menu.Item("stealToggle").GetValue<bool>()) && !v.IsIllusion)
                            {
                                if (soulring != null && soulring.CanBeCasted() && Utils.SleepCheck("soulring") &&
                                    me.Mana < me.Spellbook.Spell4.ManaCost &&
                                    me.Mana + 150 > me.Spellbook.Spell4.ManaCost)
                                {
                                    soulring.UseAbility();
                                    Utils.Sleep(Game.Ping, "soulring");
                                }
                                if (arcane != null && arcane.CanBeCasted() && Utils.SleepCheck("arcane") &&
                                    me.Mana < me.Spellbook.Spell4.ManaCost &&
                                    me.Mana + 135 > me.Spellbook.Spell4.ManaCost)
                                {
                                    arcane.UseAbility();
                                    Utils.Sleep(Game.Ping, "arcane");
                                }
                                if (arcane != null && soulring != null && Utils.SleepCheck("arcane") &&
                                    arcane.CanBeCasted() && soulring.CanBeCasted() &&
                                    me.Mana < me.Spellbook.Spell4.ManaCost &&
                                    me.Mana + 285 > me.Spellbook.Spell4.ManaCost)
                                {
                                    arcane.UseAbility();
                                    soulring.UseAbility();
                                    Utils.Sleep(Game.Ping, "arcane");
                                }
                                if (me.Mana > me.Spellbook.Spell4.ManaCost)
                                {
                                    me.Spellbook.Spell4.UseAbility();
                                    Utils.Sleep(300, "killstealR");
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
            me = ObjectMgr.LocalHero;

            ParticleEffect effect;
            ParticleEffect scope;
            #region cleanup old stuff

            if (!Game.IsInGame || me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Zuus)
            {
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
                return;
            }

            #endregion

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
            if (target == null || !target.IsAlive || target.IsInvul() || !Menu.Item("drawtargetglow").GetValue<bool>() || target.NetworkName.Replace("CDOTA_Unit_Hero_", "") != heronametargeted)
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

            if (Menu.Item("Wrealrange").GetValue<Slider>().Value != Wdrawn)
            {
                Wdrawn = Menu.Item("Wrealrange").GetValue<Slider>().Value;
                if (Effect.TryGetValue(1, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(1);
                }
                if (!Effect.TryGetValue(1, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    effect.SetControlPoint(1, new Vector3(Menu.Item("Wrealrange").GetValue<Slider>().Value, 0, 0));
                    Effect.Add(1, effect);
                }
            }

            if (Menu.Item("drawWrange").GetValue<bool>())
            {
                if (!Effect.TryGetValue(1, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    Wdrawn = Menu.Item("Wrealrange").GetValue<Slider>().Value;
                    effect.SetControlPoint(1, new Vector3(Menu.Item("Wrealrange").GetValue<Slider>().Value, 0, 0));
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

            if (Menu.Item("saferange").GetValue<Slider>().Value != blinkdrawnr)
            {
                blinkdrawnr = Menu.Item("saferange").GetValue<Slider>().Value;
                if (Effect.TryGetValue(2, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(2);
                }
                if (!Effect.TryGetValue(2, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    effect.SetControlPoint(1, new Vector3(Menu.Item("saferange").GetValue<Slider>().Value + 1200, 0, 0));
                    Effect.Add(2, effect);
                }
            }

            if (Menu.Item("drawblinkrange").GetValue<bool>())
            {
                if (!Effect.TryGetValue(2, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    blinkdrawnr = Menu.Item("saferange").GetValue<Slider>().Value;
                    effect.SetControlPoint(1, new Vector3(Menu.Item("saferange").GetValue<Slider>().Value + 1200, 0, 0));
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

            if (Menu.Item("drawQrange").GetValue<bool>())
            {
                if (!Effect.TryGetValue(3, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    effect.SetControlPoint(1, new Vector3(850, 0, 0));
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
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame)
                return;

            var player = ObjectMgr.LocalPlayer;
            me = ObjectMgr.LocalHero;
            if (player == null || player.Team == Team.Observer || me.ClassID != ClassID.CDOTA_Unit_Hero_Zuus)
                return;

            if (Menu.Item("active").GetValue<KeyBind>().Active)
            {
                DrawBox(2, 37, 110, 20, 1, new ColorBGRA(0, 200, 100, 100));
                DrawFilledBox(2, 37, 110, 20, new ColorBGRA(0, 0, 0, 100));
                DrawShadowText("Zeus#: Comboing!", 4, 37, Color.LightBlue, _text);
            }

            if (drawStealNotice && !Menu.Item("confirmSteal").GetValue<KeyBind>().Active &&
                !Menu.Item("stealToggle").GetValue<bool>())
            {
                DrawShadowText(
                    "PRESS [" + Utils.KeyToText(Menu.Item("confirmSteal").GetValue<KeyBind>().Key) + "] FOR STEAL " + steallableHero +
                    "!", 7, 400, Color.Yellow, _notice);
            }
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
    }
}


