using System;
using UnityEngine;

namespace Axiom.Physics.Units
{
    // ─────────────────────────────────────────────
    //  CONVERSIONS
    // ─────────────────────────────────────────────


    public static class UnitsConverter
    {
        // Power
        public static float HorsepowerToKilowatts(float hp)
        {
            return hp * 0.7457f;
        }

        public static float KilowattsToHorsepower(float kw)
        {
            return kw / 0.7457f;
        }

        // Mass
        public static float TonnesToKilograms(float tonnes)
        {
            return tonnes * 1000f;
        }

        public static float KilogramsToTonnes(float kg)
        {
            return kg / 1000f;
        }

        // Torque
        public static float FootPoundsToNewtonMeters(float ftLb)
        {
            return ftLb * 1.35581795f;
        }

        public static float NewtonMetersToFootPounds(float nm)
        {
            return nm / 1.35581795f;
        }

        // Speed

        public static float KnotsToMetersPerSecond(float knots)
        {
            return knots * 0.514444f;
        }

        public static float MetersPerSecondToKnots(float mps)
        {
            return mps / 0.514444f;
        }

        public static float MilesPerHourToMetersPerSecond(float mph)
        {
            return mph * 0.44704f;
        }
        public static float MetersPerSecondToMilesPerHour(float mps)
        {
            return mps / 0.44704f;
        }

        public static float KilometersPerHourToMetersPerSecond(float kph)
        {
            return kph * 0.277778f;
        }

        public static float MetersPerSecondToKilometersPerHour(float mps)
        {
            return mps / 0.277778f;
        }

        // Fuel

        // (US Gallons)
        public static float LitresToGallonsUS(float litres)
        {
            return litres * 0.264172f;
        }

        public static float GallonsUSToLitres(float gallons)
        {
            return gallons * 3.78541f;
        }

        // (Imperial Gallons)
        public static float LitresToGallonsImperial(float litres)
        {
            return litres * 0.219969f;
        }

        public static float GallonsImperialToLitres(float gallons)
        {
            return gallons * 4.54609f;
        }

        // Distance
        public static float FeetToMeters(float feet)
        {
            return feet * 0.3048f;
        }

        public static float MetersToFeet(float meters)
        {
            return meters / 0.3048f;
        }

        public static float MilesToMeters(float miles)
        {
            return miles * 1609.34f;
        }

        public static float MetersToMiles(float meters)
        {
            return meters / 1609.34f;
        }

        public static float KilometersToMeters(float km)
        {
            return km * 1000f;
        }

        public static float MetersToKilometers(float meters)
        {
            return meters / 1000f;
        }

        // Angle
        public static float DegreesToRadians(float degrees)
        {
            return degrees * Mathf.Deg2Rad;
        }

        public static float RadiansToDegrees(float radians)
        {
            return radians * Mathf.Rad2Deg;
        }

        // Angular Velocity
        public static float DegreesPerSecondToRadiansPerSecond(float dps)
        {
            return dps * Mathf.Deg2Rad;
        }

        public static float RadiansPerSecondToDegreesPerSecond(float rps)
        {
            return rps * Mathf.Rad2Deg;
        }

        public static float RPMToRadiansPerSecond(float rpm)
        {
            return rpm * (Mathf.PI * 2f / 60f);
        }

        public static float RadiansPerSecondToRPM(float rps)
        {
            return rps * (60f / (Mathf.PI * 2f));
        }

        // Acceleration
        public static float GForceToMetersPerSecondSquared(float g)
        {
            return g * 9.80665f;
        }

        public static float MetersPerSecondSquaredToGForce(float ms2)
        {
            return ms2 / 9.80665f;
        }

        // Area
        public static float SquareFeetToSquareMeters(float squareFeet)
        {
            const float feetToMeters = 0.3048f;
            return squareFeet * (feetToMeters * feetToMeters);
        }

        public static float SquareKilometersToSquareMeters(float squareKilometers)
        {
            const float kmToMeters = 1000f;
            return squareKilometers * (kmToMeters * kmToMeters);
        }

        public static float SquareMilesToSquareMeters(float squareMiles)
        {
            const float milesToMeters = 1609.344f;
            return squareMiles * (milesToMeters * milesToMeters);
        }

        public static float PoundsPerCubicFootToKgPerCubicMeter(float poundsPerCubicFoot)
        {
            const float poundsToKg = 0.45359237f;
            const float feetToMeters = 0.3048f;

            float cubicFeetToCubicMeters = feetToMeters * feetToMeters * feetToMeters;

            return poundsPerCubicFoot * (poundsToKg / cubicFeetToCubicMeters);
        }

        // Litres → Cubic Meters
        public static float LitresToCubicMeters(float litres)
        {
            return litres / 1000f;
        }

        // Cubic Meters → Litres
        public static float CubicMetersToLitres(float cubicMeters)
        {
            return cubicMeters * 1000f;
        }

        // Cubic Feet → Cubic Meters
        public static float CubicFeetToCubicMeters(float cubicFeet)
        {
            return cubicFeet * 0.0283168f;
        }

        // Cubic Meters → Cubic Feet
        public static float CubicMetersToCubicFeet(float cubicMeters)
        {
            return cubicMeters / 0.0283168f;
        }

        //Torque Per Angle

        // Newton‑meters per degree → Newton‑meters per radian
        public static float NewtonMetersPerDegreeToNewtonMetersPerRadian(float nmPerDeg)
        {
            return nmPerDeg * Mathf.Deg2Rad;
        }

        // Newton‑meters per radian → Newton‑meters per degree
        public static float NewtonMetersPerRadianToNewtonMetersPerDegree(float nmPerRad)
        {
            return nmPerRad * Mathf.Rad2Deg;
        }




    }

    // ─────────────────────────────────────────────
    // ENUMS
    // ─────────────────────────────────────────────


    //Future use case could use smaller measurements for toy size vehicles, I am very amused by centimeters per second as a speed value for instance 
    public enum PowerUnit
    {
        Kilowatts, Horsepower
    }

    public enum MassUnit
    {
        Kilograms, Tonnes
    }

    public enum TorqueUnit
    {
        NewtonMeters, FootPounds
    }

    public enum ForceUnit
    {
        Newtons
    }

    public enum ForcePerMeterUnit
    {
        NewtonsPerMeter
    }

    public enum DampingUnit
    {
        NewtonSecondsPerMeter
    }

    public enum AngularDampingUnit
    {
        NewtonMeterSecondsPerRadian
    }

    public enum TorquePerAngleUnit
    {
        NewtonMetersPerRadian,
        NewtonMetersPerDegree
    }

    public enum SpeedUnit
    {
        MetersPerSecond, Knots, MilesPerHour, KilometersPerHour
    }

    public enum FuelUnit
    {
        Litres, GallonsUS, GallonsImperial
    }

    public enum DistanceUnit
    {
        Meters, Feet, Miles, Kilometers
    }

    public enum AngleUnit
    {
        Degrees,
        Radians
    }

    public enum AngularVelocityUnit
    {
        RadiansPerSecond, DegreesPerSecond, RPM
    }

    public enum AccelerationUnit
    {
        MetersPerSecondSquared, GForce
    }

    public enum AreaUnit
    {
        SquareMeters, SquareFeet, SquareKilometers, SquareMiles
    }

    public enum DensityUnit
    {
        KgPerCubicMeter, LbPerCubicFoot
    }
    
    public enum WaterType
    {
        Freshwater,
        Saltwater,
        Custom
    }

    public enum VolumeUnit
    {
        Litres, CubicMeters, CubicFeet,
    }




    //Conversion Functions

    [System.Serializable]
    public class PowerValue
    {
        public float inputValue;
        public PowerUnit unit;

        public float ValueKilowatts
        {
            get
            {
                switch (unit)
                {
                    case PowerUnit.Horsepower:
                        return UnitsConverter.HorsepowerToKilowatts(inputValue);

                    default:
                    case PowerUnit.Kilowatts:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class MassValue
    {
        public float inputValue;
        public MassUnit unit;

        public float ValueKilograms
        {
            get
            {
                switch (unit)
                {
                    case MassUnit.Tonnes:
                        return UnitsConverter.TonnesToKilograms(inputValue);

                    default:
                    case MassUnit.Kilograms:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class TorqueValue
    {
        public float inputValue;
        public TorqueUnit unit;

        public float ValueNewtonMeters
        {
            get
            {
                switch (unit)
                {
                    case TorqueUnit.FootPounds:
                        return UnitsConverter.FootPoundsToNewtonMeters(inputValue);

                    default:
                    case TorqueUnit.NewtonMeters:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class SpeedValue
    {
        public float inputValue;
        public SpeedUnit unit;

        public float ValueMetersPerSecond
        {
            get
            {
                switch (unit)
                {
                    case SpeedUnit.Knots:
                        return UnitsConverter.KnotsToMetersPerSecond(inputValue);

                    case SpeedUnit.MilesPerHour:
                        return UnitsConverter.MilesPerHourToMetersPerSecond(inputValue);

                    case SpeedUnit.KilometersPerHour:
                        return UnitsConverter.KilometersPerHourToMetersPerSecond(inputValue);

                    default:
                    case SpeedUnit.MetersPerSecond:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class FuelValue
    {
        public float inputValue;
        public FuelUnit unit;

        public float ValueLitres
        {
            get
            {
                switch (unit)
                {
                    case FuelUnit.GallonsUS:
                        return UnitsConverter.GallonsUSToLitres(inputValue);

                    case FuelUnit.GallonsImperial:
                        return UnitsConverter.GallonsImperialToLitres(inputValue);

                    default:
                    case FuelUnit.Litres:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class DistanceValue
    {
        public float inputValue;
        public DistanceUnit unit;

        public float ValueMeters
        {
            get
            {
                switch (unit)
                {
                    case DistanceUnit.Feet:
                        return UnitsConverter.FeetToMeters(inputValue);

                    case DistanceUnit.Miles:
                        return UnitsConverter.MilesToMeters(inputValue);

                    case DistanceUnit.Kilometers:
                        return UnitsConverter.KilometersToMeters(inputValue);

                    default:
                    case DistanceUnit.Meters:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class AngleValue
    {
        public float inputValue;
        public AngleUnit unit;

        public float ValueRadians
        {
            get
            {
                switch (unit)
                {
                    case AngleUnit.Degrees:
                        return UnitsConverter.DegreesToRadians(inputValue);

                    default:
                    case AngleUnit.Radians:
                        return inputValue;
                }
            }
        }

        public float ValueDegrees
        {
            get
            {
                switch (unit)
                {
                    case AngleUnit.Radians:
                        return UnitsConverter.RadiansToDegrees(inputValue);

                    default:
                    case AngleUnit.Degrees:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class AngularVelocityValue
    {
        public float inputValue;
        public AngularVelocityUnit unit;

        public float ValueRadiansPerSecond
        {
            get
            {
                switch (unit)
                {
                    case AngularVelocityUnit.DegreesPerSecond:
                        return UnitsConverter.DegreesPerSecondToRadiansPerSecond(inputValue);

                    case AngularVelocityUnit.RPM:
                        return UnitsConverter.RPMToRadiansPerSecond(inputValue);

                    default:
                    case AngularVelocityUnit.RadiansPerSecond:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class AccelerationValue
    {
        public float inputValue;
        public AccelerationUnit unit;

        public float ValueMetersPerSecondSquared
        {
            get
            {
                switch (unit)
                {
                    case AccelerationUnit.GForce:
                        return UnitsConverter.GForceToMetersPerSecondSquared(inputValue);

                    default:
                    case AccelerationUnit.MetersPerSecondSquared:
                        return inputValue;
                }
            }
        }
    }

    [Serializable]
    public class AreaValue
    {
        public float inputValue = 1f;
        public AreaUnit unit = AreaUnit.SquareMeters;

        public float ValueSquareMeters
        {
            get
            {
                switch (unit)
                {
                    case AreaUnit.SquareFeet:
                        return UnitsConverter.SquareFeetToSquareMeters(inputValue);

                    case AreaUnit.SquareKilometers:
                        return UnitsConverter.SquareKilometersToSquareMeters(inputValue);

                    case AreaUnit.SquareMiles:
                        return UnitsConverter.SquareMilesToSquareMeters(inputValue);

                    default:
                    case AreaUnit.SquareMeters:
                        return inputValue;
                }
            }
        }
    }

[Serializable]
    public class DensityValue
    {
        public WaterType waterType = WaterType.Saltwater;

        [Tooltip("Only used when WaterType = Custom")]
        public float inputValue = 1025f;

        public DensityUnit unit = DensityUnit.KgPerCubicMeter;

        public float ValueKgPerCubicMeter
        {
            get
            {
                float baseValue;

                switch (waterType)
                {
                    case WaterType.Freshwater:
                        baseValue = 1000f; // kg/m³
                        break;

                    case WaterType.Saltwater:
                        baseValue = 1025f; // kg/m³
                        break;

                    default:
                    case WaterType.Custom:
                        baseValue = inputValue;
                        break;
                }

                switch (unit)
                {
                    case DensityUnit.LbPerCubicFoot:
                        return UnitsConverter.PoundsPerCubicFootToKgPerCubicMeter(baseValue);

                    default:
                    case DensityUnit.KgPerCubicMeter:
                        return baseValue;
                }
            }
        }
    }

    [System.Serializable]
    public class ForcePerMeterValue
    {
        public float inputValue;
        public ForcePerMeterUnit unit;

        public float ValueNewtonsPerMeter
        {
            get
            {
                switch (unit)
                {
                    default:
                    case ForcePerMeterUnit.NewtonsPerMeter:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class DampingValue
    {
        public float inputValue;
        public DampingUnit unit;

        public float ValueNewtonSecondsPerMeter
        {
            get
            {
                switch (unit)
                {
                    default:
                    case DampingUnit.NewtonSecondsPerMeter:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class AngularDampingValue
    {
        public float inputValue;
        public AngularDampingUnit unit;

        public float ValueNewtonMeterSecondsPerRadian
        {
            get
            {
                switch (unit)
                {
                    default:
                    case AngularDampingUnit.NewtonMeterSecondsPerRadian:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class TorquePerAngleValue
    {
        public float inputValue;
        public TorquePerAngleUnit unit;

        public float ValueNewtonMetersPerRadian
        {
            get
            {
                switch (unit)
                {
                    case TorquePerAngleUnit.NewtonMetersPerDegree:
                        return inputValue * Mathf.Deg2Rad;

                    default:
                    case TorquePerAngleUnit.NewtonMetersPerRadian:
                        return inputValue;
                }
            }
        }
    }

    [System.Serializable]
    public class VolumeValue
    {
        public float inputValue;
        public VolumeUnit unit;

        public float ValueCubicMeters
        {
            get
            {
                switch (unit)
                {
                    case VolumeUnit.Litres:
                        return inputValue / 1000f;

                    case VolumeUnit.CubicFeet:
                        return inputValue * 0.0283168f;

                    default:
                    case VolumeUnit.CubicMeters:
                        return inputValue;
                }
            }
        }
    }


}


