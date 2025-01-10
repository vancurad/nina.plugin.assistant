using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Astrometry;
using FluentAssertions;
using Moq;
using NINA.Plugin.Assistant.Test.Plan;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using NINA.Core.Model;

namespace NINA.Plugin.Assistant.Test.Astrometry {

    [TestFixture]
    public class OverheadObstacleAvoidanceTest
    {
        [Test]
        public void ObstacleAvoidance1() {
            Mock<IProfile> profileMock = new Mock<IProfile>();
            profileMock.SetupProperty(m => m.AstrometrySettings.Latitude, TestUtil.TEST_LOCATION_1.Latitude);
            profileMock.SetupProperty(m => m.AstrometrySettings.Longitude, TestUtil.TEST_LOCATION_1.Longitude);
            profileMock.SetupProperty(m => m.AstrometrySettings.Elevation, TestUtil.TEST_LOCATION_1.Elevation);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp1.SetupProperty(m => m.HorizonDefinition, new HorizonDefinition(null, 0, 0, 40.5)); // Horizon with a wide 40.5 degree overhead obstruction radius

            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.SetupProperty(m => m.Project, pp1.Object);

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            OverheadObstacleAvoidance testSubject = new OverheadObstacleAvoidance(profileMock.Object);
            testSubject.InterceptsObstacle(pt.Object, pf.Object, M42Crossing44Deg()).Should().BeTrue(); // 49.5deg up, just above obstruction
        }

        [Test]
        public void ObstacleAvoidance2()
        {
            Mock<IProfile> profileMock = new Mock<IProfile>();
            profileMock.SetupProperty(m => m.AstrometrySettings.Latitude, TestUtil.TEST_LOCATION_1.Latitude);
            profileMock.SetupProperty(m => m.AstrometrySettings.Longitude, TestUtil.TEST_LOCATION_1.Longitude);
            profileMock.SetupProperty(m => m.AstrometrySettings.Elevation, TestUtil.TEST_LOCATION_1.Elevation);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp1.SetupProperty(m => m.HorizonDefinition, new HorizonDefinition(null, 0, 0, 40.5));

            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.SetupProperty(m => m.Project, pp1.Object);

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            OverheadObstacleAvoidance testSubject = new OverheadObstacleAvoidance(profileMock.Object);
            testSubject.InterceptsObstacle(pt.Object, pf.Object, M42Crossing44Deg().AddHours(-1)).Should().BeFalse(); // Not above obstruction yet
        }

        [Test]
        public void ObstacleAvoidance3()
        {
            Mock<IProfile> profileMock = new Mock<IProfile>();
            profileMock.SetupProperty(m => m.AstrometrySettings.Latitude, TestUtil.TEST_LOCATION_1.Latitude);
            profileMock.SetupProperty(m => m.AstrometrySettings.Longitude, TestUtil.TEST_LOCATION_1.Longitude);
            profileMock.SetupProperty(m => m.AstrometrySettings.Elevation, TestUtil.TEST_LOCATION_1.Elevation);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp1.SetupProperty(m => m.HorizonDefinition, new HorizonDefinition(null, 0, 0, 40.5));

            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.SetupProperty(m => m.Project, pp1.Object);

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            OverheadObstacleAvoidance testSubject = new OverheadObstacleAvoidance(profileMock.Object);
            testSubject.InterceptsObstacle(pt.Object, pf.Object, M42Crossing44Deg().AddHours(-1), M42Crossing44Deg().AddHours(5)).Should().BeTrue(); // Rises above obstruction along the way
        }

        [Test]
        public void ObstacleAvoidance4()
        {
            Mock<IProfile> profileMock = new Mock<IProfile>();
            profileMock.SetupProperty(m => m.AstrometrySettings.Latitude, TestUtil.TEST_LOCATION_1.Latitude);
            profileMock.SetupProperty(m => m.AstrometrySettings.Longitude, TestUtil.TEST_LOCATION_1.Longitude);
            profileMock.SetupProperty(m => m.AstrometrySettings.Elevation, TestUtil.TEST_LOCATION_1.Elevation);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp1.SetupProperty(m => m.HorizonDefinition, new HorizonDefinition(null, 0, 0, 40.5));

            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.SetupProperty(m => m.Project, pp1.Object);

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            OverheadObstacleAvoidance testSubject = new OverheadObstacleAvoidance(profileMock.Object);
            testSubject.TimeToInterceptObstacle(pt.Object, pf.Object, M42Crossing44Deg().AddHours(-1), M42Crossing44Deg().AddHours(5)).Should().Be(TimeSpan.FromMinutes(55)); // Rises above obstruction along the way
        }

        private void AssertTimeInterval(TimeInterval interval, DateTime expectedStart, DateTime expectedEnd) {
            TimeSpan precision = TimeSpan.FromSeconds(1);
            interval.StartTime.Should().BeCloseTo(expectedStart, precision);
            interval.EndTime.Should().BeCloseTo(expectedEnd, precision);
        }

        private DateTime M42Crossing44Deg()
        {
            return new DateTime(2024, 12, 31, 20, 0, 0);
        }
    }
}