using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord;
using Assistant.NINAPlugin.Plan;
using NINA.Astrometry;
using NINA.Profile.Interfaces;

namespace Assistant.NINAPlugin.Astrometry
{
    public class OverheadObstacleAvoidance
    {
        private ObserverInfo observerInfo;

        public OverheadObstacleAvoidance(IProfile activeProfile)
        {
            this.observerInfo = new ObserverInfo
            {
                Latitude = activeProfile.AstrometrySettings.Latitude,
                Longitude = activeProfile.AstrometrySettings.Longitude,
                Elevation = activeProfile.AstrometrySettings.Elevation,
            };
        }

        public bool InterceptsObstacle(IPlanTarget planTarget, IPlanExposure planExposure, DateTime atTime)
        {
            return InterceptsObstacle(planTarget, planExposure, atTime, atTime);
        }

        public bool InterceptsObstacle(IPlanTarget planTarget, IPlanExposure planExposure, DateTime fromTime, DateTime toTime)
        {
            return TimeToInterceptObstacle(planTarget, planExposure, fromTime, toTime).TotalSeconds >= 0;
        }

        public TimeSpan TimeToInterceptObstacle(IPlanTarget planTarget, IPlanExposure planExposure, DateTime fromTime, DateTime toTime)
        {
            double deltaT = Math.Max(planExposure.ExposureLength, 5.0 * 60);
            // Precision depends on exposure length. Iterating over each "exposure" and check altitudes. Cap at 5 minute exposures min.
            for (DateTime dt = fromTime; dt.IsLessThanOrEqual(toTime); dt = dt.AddSeconds(deltaT))
            {
                double altitude = AstrometryUtils.GetAltitude(observerInfo, planTarget.Coordinates, dt);
                if (altitude > planTarget.Project.HorizonDefinition.GetFixedMaximumAltitude())
                {
                    return dt.Subtract(fromTime);
                }
            }

            return TimeSpan.FromSeconds(-1);
        }

        public TimeSpan TimeToSurpassObstacle(IPlanTarget planTarget, IPlanExposure planExposure, DateTime fromTime, DateTime toTime)
        {
            double deltaT = Math.Max(planExposure.ExposureLength, 5.0 * 60);
            // Precision depends on exposure length. Iterating over each "exposure" and check altitudes. Cap at 5 minute exposures min.
            for (DateTime dt = toTime; dt.IsGreaterThanOrEqual(fromTime); dt = dt.AddSeconds(-deltaT))
            {
                double altitude = AstrometryUtils.GetAltitude(observerInfo, planTarget.Coordinates, dt);
                if (altitude > planTarget.Project.HorizonDefinition.GetFixedMaximumAltitude())
                {
                    return dt.Subtract(fromTime);
                }
            }

            return TimeSpan.FromSeconds(-1);
        }
    }
}
