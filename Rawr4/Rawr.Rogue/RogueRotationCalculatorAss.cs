﻿using System;

namespace Rawr.Rogue
{
    public class RogueRotationCalculatorAss : RogueRotationCalculator
    {
        public RogueAbilityStats BackstabStats { get; set; }
        public RogueAbilityStats MutiStats { get; set; }
        public RogueAbilityStats EnvenomStats { get; set; }


        public float BonusMaxEnergy { get; set; }
        public float BonusStealthEnergyRegen { get; set; }
        public float DPFrequencyBonus { get; set; }
        public float DurationMult { get; set; }
        public float IPFrequencyMultiplier { get; set; }

        private float[] _averageNormalCP = new float[6];
        private float[] _averageMutiCP = new float[6];
        private float[] _avgMutiNeeded = new float[6];

        public RogueRotationCalculatorAss(Character character, Stats stats, CalculationOptionsRogue calcOpts, float hasteBonus,
            float mainHandSpeed, float offHandSpeed, float mainHandSpeedNorm, float offHandSpeedNorm, float avoidedWhiteMHAttacks, float avoidedWhiteOHAttacks, float avoidedMHAttacks,
            float avoidedOHAttacks, float avoidedFinisherAttacks, float avoidedPoisonAttacks, float chanceExtraCPPerHit, float chanceExtraCPPerMutiHit, RogueAbilityStats mainHandStats,
            RogueAbilityStats offHandStats, RogueAbilityStats backstabStats, RogueAbilityStats mutiStats, RogueAbilityStats ruptStats, RogueAbilityStats envenomStats, RogueAbilityStats snDStats,
            RogueAbilityStats exposeStats, RogueAbilityStats iPStats, RogueAbilityStats dPStats, RogueAbilityStats wPStats, RogueAbilityStats venomousWoundsStats) : base (character, stats, calcOpts, hasteBonus,
            mainHandSpeed, offHandSpeed, mainHandSpeedNorm, offHandSpeedNorm, avoidedWhiteMHAttacks, avoidedWhiteOHAttacks, avoidedMHAttacks, avoidedOHAttacks, avoidedFinisherAttacks,
            avoidedPoisonAttacks, chanceExtraCPPerHit, chanceExtraCPPerMutiHit, mainHandStats, offHandStats, ruptStats, snDStats, exposeStats, iPStats, dPStats, wPStats, venomousWoundsStats)
		{
            BackstabStats = backstabStats;
            MutiStats = mutiStats;
            EnvenomStats = envenomStats;

            BonusMaxEnergy = (Char.MainHand == null || Char.OffHand == null ? false : Char.MainHand.Type == ItemType.Dagger && Char.MainHand.Type == ItemType.Dagger) ? RV.Mastery.AssassinsResolveEnergyBonus : 0f;
            BonusStealthEnergyRegen = RV.Talents.OverkillRegenMult * Talents.Overkill;
            DPFrequencyBonus = RV.Mastery.ImprovedPoisonsDPBonus;
            IPFrequencyMultiplier = RV.Mastery.ImprovedPoisonsIPFreqMult;

            #region Probability tables
            float c = ChanceExtraCPPerHit, h = (1f - c), f = CPOnFinisher, nf = (1f - f);
            _averageNormalCP[1] = 1 * (f + nf * h) + 2 * (nf * c);
            _averageNormalCP[2] = 2 * (f * h + nf * c + nf * h * h) + 3 * (f * c);
            _averageNormalCP[3] = 3 * (f * c + f * h * h + 2 * nf * c * h + nf * h * h * h) + 4 * (f * h * c + nf * c * c + nf * h * h * c);
            _averageNormalCP[4] = 4 * (2 * f * c * h + f * h * h * h + nf * c * c + 3 * nf * c * h * h + nf * h * h * h * h) + 5 * (f * c * c + f * h * h * c + 2 * nf * c * h * c + nf * h * h * h * c);
            _averageNormalCP[5] = 5 * (f * c * c + 3 * f * c * h * h + f * h * h * h * h + 3 * nf * c * c * h + 4 * nf * c * h * h * h + nf * h * h * h * h * h) + 6 * (2 * f * c * h * c + f * h * h * h * c + nf * c * c * c + 3 * nf * c * h * h * c + nf * h * h * h * h * c);

            c = ChanceExtraCPPerMutiHit; h = (1f - c);
            _averageMutiCP[1] = 1 * (f) + 2 * (nf * h) + 3 * (nf * c);
            _averageMutiCP[2] = 2 * (nf * h) + 3 * (f * h + nf * c) + 4 * (f * c);
            _averageMutiCP[3] = 3 * (f * h + nf * c) + 4 * (f * c + nf * h * h) + 5 * (nf * h * c);
            _averageMutiCP[4] = 4 * (f * c + nf * h * h) + 5 * (f * h * h + 2 * nf * h * c) + 5 * (f * h * c + nf * c * c);
            _averageMutiCP[5] = 5 * (f * h * h + 2 * nf * h * c) + 6 * (2 * f * h * c + nf * h * h * h + nf * c * c) + 7 * (f * c * c + nf * h * h * c);

            _avgMutiNeeded[1] = 1 * (nf);
            _avgMutiNeeded[2] = 1 * (f * h + f * c + nf * h + nf * c);
            _avgMutiNeeded[3] = 1 * (f * h + f * c + nf * c) + 2 * (nf * h);
            _avgMutiNeeded[4] = 1 * (f * c) + 2 * (f * h * h + f * h * c + nf * h * h + nf * h * c + nf * c);
            _avgMutiNeeded[5] = 2 * (f + nf * h * c + nf * c) + 3 * (nf * h * h);
            #endregion
        }

        public override RogueRotationCalculation GetRotationCalculations(float durationMult, int cPG, int recupCP, int ruptCP, bool useRS, int finisher, int envenomCP, int snDCP, int mHPoison, int oHPoison, bool useTotT, int exposeCP, bool PTRMode)
        {
            DurationMult = durationMult;
            Duration = durationMult * CalcOpts.Duration;
            UseTotT = useTotT;
            CPG = cPG;
            NumberOfStealths = getNumberStealths();
            EnergyRegen = getEnergyRegen();
            TotalEnergyAvailable = getEnergyAvailable();
            TotalCPAvailable = getCPAvailable();
            float[] _avgCP = CPG == 0 ? _averageMutiCP : _averageNormalCP;

            #region Melee
            float whiteMHAttacks = Duration / MainHandSpeed;
            float whiteOHAttacks = Duration / OffHandSpeed;
            TotalEnergyAvailable += whiteOHAttacks * (1f - AvoidedWhiteOHAttacks) * EnergyOnOHAttack;
            #endregion

            #region Combo Point Generator
            CPGCount = 0f;
            CPGEnergy = getCPGEnergy();
            CPPerCPG = getCPPerCPG();
            #endregion

            #region Slice and Dice
            float avgSnDCP = _avgCP[snDCP];
            float effSnDCP = Math.Min(RV.MaxCP, avgSnDCP);
            float snDDuration = SnDStats.DurationAverage + SnDStats.DurationPerCP * effSnDCP;
            float snDCount = getSnDCount(snDDuration);
            float snDTotalEnergy = snDCount * (SnDStats.EnergyCost - RV.Talents.RelentlessStrikesEnergyBonus * ChanceOnEnergyPerCPFinisher * effSnDCP);
            float snDCPRequired = snDCount * (avgSnDCP - CPOnFinisher);
            processFinisher(snDCPRequired, snDTotalEnergy);
            #endregion

            #region Expose Armor
            float exposeCount = 0f;
            if (exposeCP > 0)
            {
                float avgExposeCP = _avgCP[exposeCP] * (1f - ExposeCPCostMult);
                float effExposeCP = Math.Min(RV.MaxCP, avgExposeCP);
                float exposeDuration = ExposeStats.DurationAverage + ExposeStats.DurationPerCP * effExposeCP;
                exposeCount = Duration / (exposeDuration * (1f + RSBonus));
                float exposeTotalEnergy = exposeCount * (ExposeStats.EnergyCost - RV.Talents.RelentlessStrikesEnergyBonus * ChanceOnEnergyPerCPFinisher * effExposeCP);
                float exposeCPRequired = exposeCount * (Math.Max(0f, avgExposeCP - CPOnFinisher));
                processFinisher(exposeCPRequired, exposeTotalEnergy);
            }
            #endregion

            #region Damage Finishers
            float ruptCount = 0f;
            float evisCount = 0f;
            float envenomCount = 0f;
            #region Rupture
            if (ruptCP > 0)
            {
                float avgRuptCP = _avgCP[ruptCP];
                float effRuptCP = Math.Min(RV.MaxCP, avgRuptCP);
                float ruptDuration = RuptStats.DurationAverage + RuptStats.DurationPerCP * effRuptCP;
                ruptCount = Duration / ruptDuration;
                float ruptTotalEnergy = ruptCount * (RuptStats.EnergyCost - effRuptCP * (RV.Talents.RelentlessStrikesEnergyBonus * ChanceOnEnergyPerCPFinisher) -
                     ChanceOnEnergyOnGarrRuptTick * RV.Talents.VenomousWoundsEnergy * ruptDuration / RV.Rupt.TickTime);
                if (ruptTotalEnergy > 0) ruptCount *= Math.Min(1f, TotalEnergyAvailable / ruptTotalEnergy);
                float ruptCPRequired = ruptCount * (Math.Max(0f, avgRuptCP - CPOnFinisher));
                processFinisher(ruptCPRequired, ruptTotalEnergy);
            }
            float venomousWoundsCount = ruptCount * (RuptStats.DurationAverage + ruptCP * RuptStats.DurationPerCP) / RV.Rupt.TickTime;
            #endregion
            #region Envenom
            float averageEnvenomCP = _avgCP[envenomCP];
            float envenomCycleEnergy = ((averageEnvenomCP - CPOnFinisher) / CPPerCPG) * CPGEnergy + EnvenomStats.EnergyCost - 25f * ChanceOnEnergyPerCPFinisher * averageEnvenomCP;
            envenomCount = TotalEnergyAvailable / envenomCycleEnergy;
            CPGCount += envenomCount * (averageEnvenomCP - CPOnFinisher) / CPPerCPG;
            TotalEnergyAvailable = 0f;
            #endregion
            #endregion

            #region Poisons
            float mHHitCount = whiteMHAttacks * (1f - AvoidedWhiteMHAttacks) + CPGCount + evisCount + envenomCount + snDCount;
            float oHHitCount = whiteOHAttacks * (1f - AvoidedWhiteOHAttacks) + (CPGCount == 1 ? CPGCount : 0);
            float iPCount = 0f;
            float dPTicks = 0f;
            float wPCount = 0f;
            float iPProcRate = RV.IP.Chance * (1f + IPFrequencyMultiplier) / RV.IP.NormWeapSpeed;
            float dPApplyChance = RV.DP.Chance + DPFrequencyBonus;
            float envenomBuffTime = envenomCount * envenomCP + envenomCount;
            #region MainHand Poison
            if (mHPoison == 1)
                iPCount += mHHitCount * MainHandStats.Weapon._speed * iPProcRate * (Duration - envenomBuffTime + (1f + RV.Envenom.BuffDPChanceBonus) * envenomBuffTime) / Duration;
            else if (mHPoison == 2 && oHPoison != 2)
            {
                float dPStackTime = RV.DP.MaxStack * MainHandSpeed / (dPApplyChance * (1f - AvoidedPoisonAttacks) * (1f - AvoidedWhiteMHAttacks));
                float envBuffDuration = envenomCount > 0 ? RV.Envenom.BuffDuration + _avgCP[envenomCP] * RV.Envenom.BuffDurationPerCP : 0f;
                float envBuffRemainder = 0f;
                float dPStackTimeBuff = 0f;
                #region Calculate DP stack time with Envenom buff
                float dPStackTimeBuffed = (1f - ChanceOnNoDPConsumeOnEnvenom) * RV.DP.MaxStack * MainHandSpeed / ((dPApplyChance + RV.Envenom.BuffDPChanceBonus) * (1f - AvoidedPoisonAttacks) * (1f - AvoidedWhiteMHAttacks));
                if (dPStackTimeBuffed >= envBuffDuration)
                {
                    float dPStackTimeRemainder = (1f - envBuffDuration / dPStackTimeBuffed) * RV.DP.MaxStack * MainHandSpeed / (dPApplyChance * (1f - AvoidedPoisonAttacks) * (1f - AvoidedWhiteMHAttacks));
                    dPStackTimeBuff = envBuffDuration + dPStackTimeRemainder;
                }
                else
                {
                    envBuffRemainder = envBuffDuration - dPStackTimeBuffed;
                    dPStackTimeBuff = dPStackTimeBuffed;
                }
                #endregion
                float dPCountAtMaxStack = mHHitCount * dPApplyChance * (1f - AvoidedPoisonAttacks) * (Duration - dPStackTime - (dPStackTimeBuff + envBuffRemainder) * envenomCount + (1f + RV.Envenom.BuffDPChanceBonus) * envenomCount * envBuffRemainder) / Duration;
                float missedTicks = (1f - ChanceOnNoDPConsumeOnEnvenom) * envenomCount * RV.GetMissedDPTicks(dPStackTimeBuff) + RV.GetMissedDPTicks(dPStackTime);
                dPTicks = mHHitCount * dPApplyChance * (1f - AvoidedPoisonAttacks) * (Duration - envenomBuffTime) / Duration + mHHitCount * (dPApplyChance + RV.Envenom.BuffDPChanceBonus) * (1f - AvoidedPoisonAttacks) * envenomBuffTime / Duration - missedTicks;
                if (oHPoison == 1)
                    iPCount += dPCountAtMaxStack;
                else if (oHPoison == 3)
                    wPCount += dPCountAtMaxStack;
            }
            else if (mHPoison == 3)
                wPCount += mHHitCount * MainHandStats.Weapon._speed * RV.WP.Chance / RV.WP.NormWeapSpeed;
            #endregion
            #region OffHand Poison
            if (oHPoison == 1)
                iPCount += oHHitCount * OffHandStats.Weapon._speed * iPProcRate * (Duration - envenomBuffTime + (1f + RV.Envenom.BuffDPChanceBonus) * envenomBuffTime) / Duration;
            else if (oHPoison == 2 && mHPoison != 2)
            {
                float dPStackTime = RV.DP.MaxStack * OffHandSpeed / (dPApplyChance * (1f - AvoidedPoisonAttacks) * (1f - AvoidedWhiteMHAttacks));
                float envBuffDuration = envenomCount > 0 ? RV.Envenom.BuffDuration + _avgCP[envenomCP] * RV.Envenom.BuffDurationPerCP : 0f;
                float envBuffRemainder = 0f;
                float dPStackTimeBuff = 0f;
                #region Calculate DP stack time with Envenom buff
                float dPStackTimeBuffed = (1f - ChanceOnNoDPConsumeOnEnvenom) * RV.DP.MaxStack * OffHandSpeed / ((dPApplyChance + RV.Envenom.BuffDPChanceBonus) * (1f - AvoidedPoisonAttacks) * (1f - AvoidedWhiteOHAttacks));
                if (dPStackTimeBuffed >= envBuffDuration)
                {
                    float dPStackTimeRemainder = (1f - envBuffDuration / dPStackTimeBuffed) * RV.DP.MaxStack * OffHandSpeed / (dPApplyChance * (1f - AvoidedPoisonAttacks) * (1f - AvoidedWhiteOHAttacks));
                    dPStackTimeBuff = envBuffDuration + dPStackTimeRemainder;
                }
                else
                {
                    envBuffRemainder = envBuffDuration - dPStackTimeBuffed;
                    dPStackTimeBuff = dPStackTimeBuffed;
                }
                #endregion
                float dPCountAtMaxStack = oHHitCount * dPApplyChance * (1f - AvoidedPoisonAttacks) * (Duration - dPStackTime - (dPStackTimeBuff + envBuffRemainder) * envenomCount + (1f + RV.Envenom.BuffDPChanceBonus) * envenomCount * envBuffRemainder) / Duration;
                float missedTicks = (1f - ChanceOnNoDPConsumeOnEnvenom) * envenomCount * RV.GetMissedDPTicks(dPStackTimeBuff) + RV.GetMissedDPTicks(dPStackTime);
                dPTicks = oHHitCount * dPApplyChance * (1f - AvoidedPoisonAttacks) * (Duration - envenomBuffTime) / Duration + oHHitCount * (dPApplyChance + RV.Envenom.BuffDPChanceBonus) * (1f - AvoidedPoisonAttacks) * envenomBuffTime / Duration - missedTicks;
                if (oHPoison == 1)
                    iPCount += dPCountAtMaxStack;
                else if (oHPoison == 3)
                    wPCount += dPCountAtMaxStack;
            }
            else if (oHPoison == 3)
                wPCount += oHHitCount * OffHandStats.Weapon._speed * RV.WP.Chance / RV.WP.NormWeapSpeed;
            #endregion
            iPCount *= (1f - AvoidedPoisonAttacks);
            wPCount *= (1f - AvoidedPoisonAttacks);
            #endregion

            #region Damage Totals
            float mainHandDamageTotal = whiteMHAttacks * MainHandStats.DamagePerSwing;
            float offHandDamageTotal = whiteOHAttacks * OffHandStats.DamagePerSwing;
            float backstabDamageTotal = (CPG == 1 ? CPGCount : 0) * BackstabStats.DamagePerSwing;
            float hemoDamageTotal = 0;
            float sStrikeDamageTotal = 0;
            float mutiDamageTotal = (CPG == 0 ? CPGCount : 0) * MutiStats.DamagePerSwing;
            float rStrikeDamageTotal = 0;
            float ruptDamageTotal = ruptCount * RuptStats.DamagePerSwingArray[(int)Math.Floor((double)ruptCP)] + (ruptCP - (float)Math.Floor((double)ruptCP)) * (RuptStats.DamagePerSwingArray[(int)Math.Min(Math.Floor((double)ruptCP) + 1, 5)] - RuptStats.DamagePerSwingArray[(int)Math.Floor((double)ruptCP)]);
            float evisDamageTotal = 0;
            float envenomDamageTotal = envenomCount * (EnvenomStats.DamagePerSwing + EnvenomStats.DamagePerSwingPerCP * Math.Min(_avgCP[envenomCP], 5));
            float instantPoisonTotal = iPCount * IPStats.DamagePerSwing;
            float deadlyPoisonTotal = dPTicks * DPStats.DamagePerSwing;
            float woundPoisonTotal = wPCount * WPStats.DamagePerSwing;
            float venomousWoundsTotal = venomousWoundsCount * VenomousWoundsStats.DamagePerSwing;

            float damageTotal = (mainHandDamageTotal + offHandDamageTotal + backstabDamageTotal + hemoDamageTotal + sStrikeDamageTotal + mutiDamageTotal + rStrikeDamageTotal + 
                ruptDamageTotal + evisDamageTotal + envenomDamageTotal + instantPoisonTotal + deadlyPoisonTotal + woundPoisonTotal + venomousWoundsTotal);
            #endregion

            return new RogueRotationCalculation()
            {
                Duration = Duration,
                MultipleSegments = DurationMult < 1f,
                TotalDamage = damageTotal,
                DPS = damageTotal / Duration,

                MainHandCount = whiteMHAttacks,
                OffHandCount = whiteOHAttacks,
                BackstabCount = (CPG == 1 ? CPGCount : 0),
                MutiCount = (CPG == 0 ? CPGCount : 0),
                RuptCount = ruptCount,
                EnvenomCount = envenomCount,
                SnDCount = snDCount,
                EACount = exposeCount,
                IPCount = iPCount,
                DPCount = dPTicks,
                VenomousWoundsCount = venomousWoundsCount,

                FinisherCP = envenomCP,
                EvisCP = 0,
                EnvenomCP = Math.Min(_avgCP[envenomCP], 5),
                RuptCP = ruptCP,
                SnDCP = snDCP,

                MHPoison = mHPoison,
                OHPoison = oHPoison,

                UseTotT = useTotT,
                CutToTheChase = ChanceOnSnDResetOnEvisEnv,
            };
        }

        public override float getEnergyAvailable()
        {
            return RV.BaseEnergy + BonusMaxEnergy + EnergyRegen * Duration +
                (UseTotT ? (-RV.TotT.Cost + ToTTCostReduction) * (Duration - RV.TotT.Duration) / RV.TotT.CD : 0f) + 
                NumberOfStealths * RV.Talents.OverkillRegenDuration * EnergyRegen * (1f + BonusStealthEnergyRegen) + 
                RV.ColdBlood.EnergyBonus * Talents.ColdBlood * Duration / RV.ColdBlood.CD;
        }

        public override float getCPGEnergy()
        {
            return ((CPG == 1 && DurationMult == RV.Talents.MurderousIntentThreshold) ? BackstabStats.EnergyCost - EnergyOnBelow35BS : MutiStats.EnergyCost);
        }

        public override float getCPPerCPG()
        {
            return (CPG == 1 ? BackstabStats.CPPerSwing : MutiStats.CPPerSwing);
        }

        public override float getSnDCount(float snDDuration)
        {
            float snDCount = Duration / snDDuration;
            return Math.Max(1f, snDCount * (1f - ChanceOnSnDResetOnEvisEnv));
        }
    }
}