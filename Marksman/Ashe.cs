﻿#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

#endregion

namespace Marksman
{

    internal class Ashe : Champion
    {
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public Ashe()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1200);
            E = new Spell(SpellSlot.E, 2500);
            R = new Spell(SpellSlot.R, 20000);
            W.SetSkillshot(250f, 24.32f, 902f, true, SkillshotType.SkillshotCone);
            E.SetSkillshot(377f, 299f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(250f, 130f, 1600f, false, SkillshotType.SkillshotLine);
            Interrupter.OnPossibleToInterrupt += Game_OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Orbwalking.OnAttack += Orbwalking_OnAttack;
            Utils.PrintMessage("Ashe loaded.");
        }

        public void Game_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (R.IsReady() && Config.Item("RInterruptable").GetValue<bool>() && unit.IsValidTarget(1500))
            {
                R.Cast(unit);
            }
        }

        public void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!Config.Item("EFlash").GetValue<bool>() || unit.Team != ObjectManager.Player.Team || unit.Type != ObjectManager.Player.Type) return;

            if (spell.SData.Name == "SummonerFlash")
                E.Cast(spell.End);
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            var drawW = Config.Item("DrawW").GetValue<Circle>();
            if (drawW.Active)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, W.Range, drawW.Color);
            }

            var drawE = Config.Item("DrawE").GetValue<Circle>();
            if (drawE.Active)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, E.Range, drawE.Color);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            //Combo
            if (ComboActive)
            {
                var target = SimpleTs.GetTarget(1200, SimpleTs.DamageType.Physical);
                if (target == null) return;

                if (!Config.Item("QExploit").GetValue<bool>() && !IsQActive() && Config.Item("UseQC").GetValue<bool>())
                    Q.Cast();

                if (Config.Item("UseWC").GetValue<bool>() && W.IsReady())
                    W.Cast(target);

                if (Config.Item("UseRC").GetValue<bool>() && R.IsReady())
                {
                    var rTarget = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Physical);

                    if (!rTarget.IsValidTarget() ||
                        !(ObjectManager.Player.GetSpellDamage(rTarget, SpellSlot.R) > rTarget.Health)) return;

                    R.Cast(rTarget);
                }
            }

            //Harass
            if (HarassActive)
            {
                var target = SimpleTs.GetTarget(1200, SimpleTs.DamageType.Physical);
                if (target == null) return;

                if (!Config.Item("QExploit").GetValue<bool>() && !IsQActive() && Config.Item("UseQH").GetValue<bool>())
                    Q.Cast();

                if (Config.Item("UseWH").GetValue<bool>() && W.IsReady())
                    W.Cast(target);
            }

            //Lane Clear
            if (LaneClearActive && Config.Item("DeactivateQ").GetValue<bool>() && IsQActive())
                Q.Cast();

            //Manual cast R
            if (Config.Item("RManualCast").GetValue<KeyBind>().Active)
            {
                var rTarget = SimpleTs.GetTarget(2000, SimpleTs.DamageType.Physical);
                R.Cast(rTarget);
            }
        }

        public override void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (LaneClearActive && Config.Item("DeactivateQ").GetValue<bool>()) return;

            if ((Config.Item("QExploit").GetValue<bool>() && !IsQActive()))
                Q.Cast();
        }

        public void Orbwalking_OnAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (!unit.IsMe) return;
            if (LaneClearActive && Config.Item("DeactivateQ").GetValue<bool>()) return;

            if (Config.Item("QExploit").GetValue<bool>() && IsQActive())
                Q.Cast();
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC", "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC", "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseRC", "Use R").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH", "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWH", "Use W").SetValue(true));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("DeactivateQ", "Always deactivate Frost Arrow").SetValue(false));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawW", "W range").SetValue(new Circle(true, Color.CornflowerBlue)));
            config.AddItem(
                new MenuItem("DrawE", "E range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("QExploit", "Use Q Exploit").SetValue(true));
            config.AddItem(new MenuItem("RInterruptable", "Auto R Interruptable Spells").SetValue(true));
            config.AddItem(new MenuItem("EFlash", "Use E against Flashes").SetValue(true));
            config.AddItem(new MenuItem("RManualCast", "Cast R Manually(2000 range)"))
                .SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press));
            return true;
        }

        public static bool IsQActive()
        {
            return ObjectManager.Player.Buffs.Where(buff => buff.Name == "FrostShot" && buff.IsActive).Select(buff => buff.IsActive).FirstOrDefault() || ObjectManager.Player.HasBuff("FrostShot");
        }
    }
}