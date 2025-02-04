﻿using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using FluentAssertions;
using FluentAssertions.Extensions;
using Moq;
using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.Test.Astrometry;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class PlannerTest {

        [Test]
        public void testFilterForReadyComplete() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            Mock<IPlanProject> pp2 = PlanMocks.GetMockPlanProject("pp2", ProjectState.Active);
            pt = PlanMocks.GetMockPlanTarget("M31", TestUtil.M31);
            pf = PlanMocks.GetMockPlanExposure("OIII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("SII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp2, pt);

            Assert.That(new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForIncomplete(null), Is.Null);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object, pp2.Object);
            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForIncomplete(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(2);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();
            IPlanExposure pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeFalse();

            pp = projects[1];
            pp.Name.Should().Be("pp2");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectComplete);
            pt1 = pp.Targets[0];
            pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeTrue();
            pf1.RejectedReason.Should().Be(Reasons.FilterComplete);
            pf1 = pt1.ExposurePlans[1];
            pf1.Rejected.Should().BeTrue();
            pf1.RejectedReason.Should().Be(Reasons.FilterComplete);
        }

        [Test]
        public void testFilterForIncomplete() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            pt = PlanMocks.GetMockPlanTarget("M31", TestUtil.M31);
            pf = PlanMocks.GetMockPlanExposure("OIII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("SII", 10, 12);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);
            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForIncomplete(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();

            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();
            IPlanExposure pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeFalse();

            IPlanTarget pt2 = pp.Targets[1];
            pt2.Rejected.Should().BeTrue();
            pt2.RejectedReason.Should().Be(Reasons.TargetComplete);

            IPlanExposure pf2 = pt2.ExposurePlans[0];
            pf2.Rejected.Should().BeTrue();
            pf2.RejectedReason.Should().Be(Reasons.FilterComplete);
            IPlanExposure pf3 = pt2.ExposurePlans[1];
            pf3.Rejected.Should().BeTrue();
            pf3.RejectedReason.Should().Be(Reasons.FilterComplete);
        }

        [Test]
        public void testFilterForIncompleteAllExposuresThrottled() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            ProfilePreference prefs = GetPrefs();

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);

            // Assert setup for throttling
            pp1.Object.EnableGrader.Should().BeFalse();
            prefs.ExposureThrottle.Should().Be(125);

            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            ExposurePlan ep = new ExposurePlan("abcd-1234") { Desired = 12, Acquired = 15 };
            ExposureTemplate et = new ExposureTemplate("abcd-1234", "R", "R");
            IPlanExposure peRed = new PlanExposure(pt.Object, ep, et);

            ep = new ExposurePlan("abcd-1234") { Desired = 12, Acquired = 15 };
            et = new ExposureTemplate("abcd-1234", "G", "G");
            IPlanExposure peGreen = new PlanExposure(pt.Object, ep, et);

            ep = new ExposurePlan("abcd-1234") { Desired = 12, Acquired = 10 };
            et = new ExposureTemplate("abcd-1234", "B", "B");
            IPlanExposure peBlue = new PlanExposure(pt.Object, ep, et);

            pt.Object.ExposurePlans = new List<IPlanExposure>() { peRed, peGreen, peBlue };

            // Blue is not complete ...
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);
            projects = new Planner(new DateTime(2023, 12, 15, 18, 0, 0), profileMock.Object.ActiveProfile, prefs, false).FilterForIncomplete(projects);
            projects.Count.Should().Be(1);
            projects[0].Rejected.Should().BeFalse();
            projects[0].Targets[0].Rejected.Should().BeFalse();

            ep = new ExposurePlan("abcd-1234") { Desired = 12, Acquired = 15 };
            et = new ExposureTemplate("abcd-1234", "B", "B");
            peBlue = new PlanExposure(pt.Object, ep, et);

            pt.Object.ExposurePlans = new List<IPlanExposure>() { peRed, peGreen, peBlue };

            // All are now complete due to throttle
            projects = new Planner(new DateTime(2023, 12, 15, 18, 0, 0), profileMock.Object.ActiveProfile, prefs, false).FilterForIncomplete(projects);
            projects.Count.Should().Be(1);
            projects[0].Rejected.Should().BeTrue();
            projects[0].RejectedReason.Should().Be(Reasons.ProjectComplete);
            projects[0].Targets[0].Rejected.Should().BeTrue();
            projects[0].Targets[0].RejectedReason.Should().Be(Reasons.TargetComplete);
        }

        [Test]
        public void testTargetNoExposurePlans() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            pt = PlanMocks.GetMockPlanTarget("M31", TestUtil.M31);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);
            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForIncomplete(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();

            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();
            IPlanExposure pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeFalse();

            IPlanTarget pt2 = pp.Targets[1];
            pt2.ExposurePlans.Count.Should().Be(0);
            pt2.Rejected.Should().BeTrue();
            pt2.RejectedReason.Should().Be(Reasons.TargetComplete);
        }

        [Test]
        public void testFilterForVisibilityNeverRises() {
            // Southern hemisphere location and IC1805
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_2);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("IC1805", TestUtil.IC1805);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForVisibility(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectAllTargets);
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeTrue();
            pt1.RejectedReason.Should().Be(Reasons.TargetNeverRises);
        }

        [Test]
        public void testFilterForVisibilityNotNow() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 6, 17, 18, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForVisibility(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectAllTargets);
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeTrue();
            pt1.RejectedReason.Should().Be(Reasons.TargetNotVisible);
        }

        [Test]
        public void testFilterForVisibilityVisible() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);

            bool captureVisibility = false;
            DateTime captureStartTime = DateTime.MinValue;
            DateTime captureCulminationTime = DateTime.MinValue;
            DateTime captureEndTime = DateTime.MinValue;
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.Setup(c => c.SetCircumstances(It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback<bool, DateTime, DateTime, DateTime>((visible, st, ct, et) => {
                    captureVisibility = visible;
                    captureStartTime = st;
                    captureCulminationTime = ct;
                    captureEndTime = et;
                });

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 19, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForVisibility(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();

            captureVisibility.Should().BeTrue();
            TimeSpan precision = TimeSpan.FromSeconds(1);
            captureStartTime.Should().BeCloseTo(new DateTime(2023, 12, 17, 18, 36, 46), precision);
            captureCulminationTime.Should().BeCloseTo(new DateTime(2023, 12, 18, 0, 5, 45), precision);
            captureEndTime.Should().BeCloseTo(new DateTime(2023, 12, 18, 5, 49, 13), precision);
        }

        [Test]
        public void testFilterForVisibilityInMeridianWindow() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp1.SetupProperty(m => m.MeridianWindow, 30);

            bool captureVisibility = false;
            DateTime captureStartTime = DateTime.MinValue;
            DateTime captureCulminationTime = DateTime.MinValue;
            DateTime captureEndTime = DateTime.MinValue;
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.Setup(c => c.SetCircumstances(It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback<bool, DateTime, DateTime, DateTime>((visible, st, ct, et) => {
                    captureVisibility = visible;
                    captureStartTime = st;
                    captureCulminationTime = ct;
                    captureEndTime = et;
                });

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 23, 36, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForVisibility(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();

            captureVisibility.Should().BeTrue();
            TimeSpan precision = TimeSpan.FromSeconds(1);
            captureStartTime.Should().BeCloseTo(new DateTime(2023, 12, 17, 23, 35, 45), precision);
            captureCulminationTime.Should().BeCloseTo(new DateTime(2023, 12, 18, 0, 5, 45), precision);
            captureEndTime.Should().BeCloseTo(new DateTime(2023, 12, 18, 0, 35, 45), precision);
        }

        [Test]
        public void testFilterForVisibilityWaitForMeridianWindow() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp1.SetupProperty(m => m.MeridianWindow, 30);

            bool captureVisibility = false;
            DateTime captureStartTime = DateTime.MinValue;
            DateTime captureCulminationTime = DateTime.MinValue;
            DateTime captureEndTime = DateTime.MinValue;
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.Setup(c => c.SetCircumstances(It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback<bool, DateTime, DateTime, DateTime>((visible, st, ct, et) => {
                    captureVisibility = visible;
                    captureStartTime = st;
                    captureCulminationTime = ct;
                    captureEndTime = et;
                });

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 19, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForVisibility(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeTrue();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeTrue();
            pt1.RejectedReason.Should().Be(Reasons.TargetBeforeMeridianWindow);

            captureVisibility.Should().BeFalse();
        }

        [Test]
        public void testFilterForVisibilityMeridianWindowCircumpolar() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_6);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp1.SetupProperty(m => m.MeridianWindow, 30);

            bool captureVisibility = false;
            DateTime captureStartTime = DateTime.MinValue;
            DateTime captureCulminationTime = DateTime.MinValue;
            DateTime captureEndTime = DateTime.MinValue;
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("IC1805", TestUtil.IC1805);
            pt.Setup(c => c.SetCircumstances(It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback<bool, DateTime, DateTime, DateTime>((visible, st, ct, et) => {
                    captureVisibility = visible;
                    captureStartTime = st;
                    captureCulminationTime = ct;
                    captureEndTime = et;
                });

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 20, 34, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForVisibility(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();

            captureVisibility.Should().BeTrue();
            TimeSpan precision = TimeSpan.FromSeconds(1);
            captureStartTime.Should().BeCloseTo(new DateTime(2023, 12, 17, 20, 33, 52), precision);
            captureCulminationTime.Should().BeCloseTo(new DateTime(2023, 12, 17, 21, 3, 52), precision);
            captureEndTime.Should().BeCloseTo(new DateTime(2023, 12, 17, 21, 33, 52), precision);
        }

        [Test]
        public void testFilterForVisibilityNotYetVisible() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForVisibility(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectAllTargets);
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeTrue();
            pt1.RejectedReason.Should().Be(Reasons.TargetNotYetVisible);
        }

        [Test]
        public void testFilterForMoonAvoidance() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.SetupProperty(m => m.StartTime, new DateTime(2023, 12, 25, 18, 9, 0));
            pt.SetupProperty(m => m.EndTime, new DateTime(2023, 12, 26, 5, 17, 0));

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("L", 10, 0);
            pf.SetupProperty(f => f.MoonAvoidanceEnabled, true);
            pf.SetupProperty(f => f.MoonAvoidanceSeparation, 50);
            pf.SetupProperty(f => f.MoonAvoidanceWidth, 7);
            PlanMocks.AddMockPlanFilter(pt, pf);

            pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            pf.SetupProperty(f => f.MoonAvoidanceEnabled, true);
            pf.SetupProperty(f => f.MoonAvoidanceSeparation, 30);
            pf.SetupProperty(f => f.MoonAvoidanceWidth, 7);
            PlanMocks.AddMockPlanFilter(pt, pf);

            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 25, 18, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForMoonAvoidance(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();

            IPlanExposure pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeTrue();
            pf1.RejectedReason.Should().Be(Reasons.FilterMoonAvoidance);

            pf1 = pt1.ExposurePlans[1];
            pf1.Rejected.Should().BeFalse();
        }

        [Test]
        public void testCheckForVisibleNowNoWait() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            DateTime atTime = new DateTime(2023, 1, 23, 18, 0, 0);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt1 = PlanMocks.GetMockPlanTarget("T1", TestUtil.M42);
            pt1.SetupProperty(t => t.StartTime, atTime.AddMinutes(-10));
            pt1.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            Mock<IPlanTarget> pt2 = PlanMocks.GetMockPlanTarget("T2", TestUtil.M42);
            pt2.SetupProperty(t => t.StartTime, atTime.AddMinutes(10));
            pt2.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));

            PlanMocks.AddMockPlanTarget(pp, pt1);
            PlanMocks.AddMockPlanTarget(pp, pt2);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp.Object);

            DateTime? wait = new Planner(atTime, profileMock.Object.ActiveProfile, GetPrefs(), false).CheckForVisibleNow(projects);
            wait.Should().BeNull();
        }

        [Test]
        public void testCheckForVisibleNowWait() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            DateTime atTime = new DateTime(2023, 1, 23, 18, 0, 0);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt1 = PlanMocks.GetMockPlanTarget("T1", TestUtil.M42);
            pt1.SetupProperty(t => t.StartTime, atTime.AddMinutes(10)); // <- should find this
            pt1.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            pt1.SetupProperty(t => t.Rejected, true);
            pt1.SetupProperty(t => t.RejectedReason, Reasons.TargetNotYetVisible);
            Mock<IPlanTarget> pt2 = PlanMocks.GetMockPlanTarget("T2", TestUtil.M42);
            pt2.SetupProperty(t => t.StartTime, atTime.AddMinutes(20));
            pt2.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            pt2.SetupProperty(t => t.Rejected, true);
            pt2.SetupProperty(t => t.RejectedReason, Reasons.TargetNotYetVisible);
            Mock<IPlanTarget> pt3 = PlanMocks.GetMockPlanTarget("T3", TestUtil.M42);
            pt3.SetupProperty(t => t.StartTime, atTime.AddMinutes(5));
            pt3.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            pt3.SetupProperty(t => t.Rejected, true);
            pt3.SetupProperty(t => t.RejectedReason, Reasons.FilterMoonAvoidance);

            PlanMocks.AddMockPlanTarget(pp, pt1);
            PlanMocks.AddMockPlanTarget(pp, pt2);
            PlanMocks.AddMockPlanTarget(pp, pt3);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp.Object);

            DateTime? wait = new Planner(atTime, profileMock.Object.ActiveProfile, GetPrefs(), false).CheckForVisibleNow(projects);
            wait.Should().BeSameDateAs(atTime.AddMinutes(10));
        }

        [Test]
        public void testSelectTargetByScore() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            Mock<IPlanProject> pp2 = PlanMocks.GetMockPlanProject("pp2", ProjectState.Active);
            pt = PlanMocks.GetMockPlanTarget("IC1805", TestUtil.IC1805);
            PlanMocks.AddMockPlanTarget(pp2, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object, pp2.Object);

            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.Setup(m => m.ScoreTarget(It.Is<IPlanTarget>(t => t.Name.Equals("IC1805")))).Returns(1);

            IPlanTarget selected = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).SelectTargetByScore(projects, scoringEngineMock.Object);
            Assert.That(selected, Is.Not.Null);
            selected.Name.Should().Be("IC1805");
            selected.Rejected.Should().BeFalse();

            IPlanTarget m42 = pp1.Object.Targets[0];
            m42.Name.Should().Be("M42");
            m42.Rejected.Should().BeTrue();
            m42.RejectedReason.Should().Be(Reasons.TargetLowerScore);
        }

        [Test]
        public void testGetTargetTimeWindow() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            int minimumMinutes = 30;
            pp.SetupProperty(p => p.MinimumTime, minimumMinutes);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            PlanMocks.AddMockPlanTarget(pp, pt);

            DateTime atTime = new DateTime(2023, 1, 23, 18, 0, 0);
            pt.SetupProperty(t => t.StartTime, atTime.AddMinutes(10));
            pt.SetupProperty(t => t.EndTime, atTime.AddMinutes(50));
            TimeInterval window = new Planner(atTime, profileMock.Object.ActiveProfile, GetPrefs(), false).GetTargetTimeWindow(false, atTime, pt.Object, null);
            window.StartTime.Should().BeSameDateAs(23.January(2023).At(18, 10, 0));
            window.EndTime.Should().BeSameDateAs(23.January(2023).At(18, 40, 0));
            window.Duration.Should().Be(minimumMinutes * 60);

            minimumMinutes = 60;
            pp.SetupProperty(p => p.MinimumTime, minimumMinutes);
            pt.SetupProperty(t => t.StartTime, atTime.AddMinutes(-10));
            pt.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            window = new Planner(atTime, profileMock.Object.ActiveProfile, GetPrefs(), false).GetTargetTimeWindow(false, atTime, pt.Object, null);
            window.StartTime.Should().BeSameDateAs(23.January(2023).At(18, 0, 0));
            window.EndTime.Should().BeSameDateAs(23.January(2023).At(19, 0, 0));
            window.Duration.Should().Be(minimumMinutes * 60);

            atTime = new DateTime(2023, 1, 24, 1, 0, 0);
            pp.SetupProperty(p => p.MeridianWindow, 120);
            TimeInterval meridianWindow = new TimeInterval(atTime.AddMinutes(-10), atTime.AddHours(3));
            pt.SetupProperty(t => t.MeridianWindow, meridianWindow);
            pt.SetupProperty(t => t.StartTime, atTime.AddMinutes(-20));
            pt.SetupProperty(t => t.EndTime, atTime.AddHours(4));
            window = new Planner(atTime, profileMock.Object.ActiveProfile, GetPrefs(), false).GetTargetTimeWindow(false, atTime, pt.Object, null);
            TimeSpan precision = TimeSpan.FromSeconds(1);
            window.StartTime.Should().BeCloseTo(atTime, precision);
            window.EndTime.Should().BeCloseTo(atTime.AddHours(3), precision);
            window.Duration.Should().Be(60 * 60 * 3);
        }

        [Test]
        public void testFilterForOverheadObstructionAtStart()
        {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_1);

            // Plan with overhead obstruction of 40.5 degree radius
            HorizonDefinition hd = new HorizonDefinition(HorizonDefinition.GetConstantHorizon(0), 0, 0, 40.5);
            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active, hd);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            // Target above obstruction at 20:00:00.
            projects = new Planner(new DateTime(2024, 12, 31, 20, 0, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForVisibility(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectAllTargets);
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeTrue();
            pt1.RejectedReason.Should().Be(Reasons.TargetNotYetVisible);
        }

        [Test]
        public void testFilterForOverheadObstructionAfterSomeTime()
        {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_1);

            // Plan with overhead obstruction of 40.5 degree radius
            HorizonDefinition hd = new HorizonDefinition(HorizonDefinition.GetConstantHorizon(0), 0, 0, 40.5);
            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active, hd);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            // Target above obstruction around 20:00:00.
            projects = new Planner(new DateTime(2024, 12, 31, 19, 45, 0), profileMock.Object.ActiveProfile, GetPrefs(), false).FilterForVisibility(projects);
            Assert.That(projects, Is.Not.Null);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.RejectedReason.Should().BeNull();
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.RejectedReason.Should().BeNull();
            pt1.Rejected.Should().BeFalse();
        }

        [Test]
        public void testHasActiveProjects() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            Mock<IPlanProject> pp2 = PlanMocks.GetMockPlanProject("pp2", ProjectState.Active);
            pt = PlanMocks.GetMockPlanTarget("M31", TestUtil.M31);
            pf = PlanMocks.GetMockPlanExposure("OIII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("SII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp2, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object, pp2.Object);
            Planner sut = new Planner(DateTime.Now, profileMock.Object.ActiveProfile, GetPrefs(), false);
            sut.HasActiveProjects(projects).Should().BeTrue();

            projects = PlanMocks.ProjectsList(pp2.Object);
            sut = new Planner(DateTime.Now, profileMock.Object.ActiveProfile, GetPrefs(), false);
            sut.HasActiveProjects(projects).Should().BeFalse();
        }

        [Test]
        public void testEmptyPlan() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp1.SetupProperty(m => m.MinimumTime, 10);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            pf.SetupProperty(m => m.ExposureLength, 600);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            DateTime start = new DateTime(2024, 9, 11, 1, 0, 0);
            Planner sut = new Planner(start, profileMock.Object.ActiveProfile, GetPrefs(), false);

            // Plan should have one exposure instruction: one 10m exposure fits in 11m time interval
            var span = new TimeInterval(start, start.AddMinutes(11));
            var instructions = sut.PlanInstructions(pt.Object, null, span);

            int expCount = 0;
            foreach (var instruction in instructions) {
                if (instruction is PlanTakeExposure) { expCount++; }
            }

            expCount.Should().Be(1);

            // But if the interval is just 10m, it should abort
            span = new TimeInterval(start, start.AddMinutes(10));
            instructions = sut.PlanInstructions(pt.Object, null, span);
            instructions.Should().BeNull();
        }

        [Test]
        [Ignore("should test in the future")]
        public void testPerfectPlan() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            DateTime atTime = new DateTime(2023, 1, 26);

            List<IPlanProject> projects = new List<IPlanProject> {
                PlanMocks.GetMockPlanProject("pp1", ProjectState.Active).Object,
                PlanMocks.GetMockPlanProject("pp2", ProjectState.Active).Object
            };

            List<SchedulerPlan> plans = Planner.GetPerfectPlan(atTime, profileMock.Object, GetPrefs(), projects);
            foreach (SchedulerPlan plan in plans) {
                TestContext.WriteLine("PLAN -----------------------------------------------------");
                TestContext.WriteLine(plan.PlanSummary());
            }
        }

        [Test]
        public void testNotEmulator() {
            // prevent commits with emulator on
            Common.USE_EMULATOR.Should().BeFalse();
        }

        private ProfilePreference GetPrefs(string profileId = "abcd-1234") {
            return new ProfilePreference(profileId);
        }
    }
}