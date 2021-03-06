﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Duracellko.PlanningPoker.Service;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using D = Duracellko.PlanningPoker.Domain;

namespace Duracellko.PlanningPoker.Test.Service
{
    [TestClass]
    public sealed class PlanningPokerHubTest : IDisposable
    {
        private const string TeamName = "test team";
        private const string ScrumMasterName = "master";
        private const string MemberName = "member";
        private const string ObserverName = "observer";

        private const string LongTeamName = "ttttttttttttttttttttttttttttttttttttttttttttttttttt";
        private const string LongMemberName = "mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm";

        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();

        [TestMethod]
        public void Constructor_PlanningPoker_PlanningPokerPropertyIsSet()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);

            // Act
            using (var result = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Verify
                Assert.AreEqual<D.IPlanningPoker>(planningPoker.Object, result.PlanningPoker);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_Null_ArgumentNullException()
        {
            // Act
            var logger = new Logger<PlanningPokerHub>(_loggerFactory);
            using (var result = new PlanningPokerHub(null, null, logger))
            {
            }
        }

        [TestMethod]
        public void CreateTeam_TeamNameAndScrumMasterName_ReturnsCreatedTeam()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.CreateScrumTeam(TeamName, ScrumMasterName)).Returns(teamLock.Object).Verifiable();

            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.CreateTeam(TeamName, ScrumMasterName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.AreEqual<string>(TeamName, result.Name);
                Assert.IsNotNull(result.ScrumMaster);
                Assert.AreEqual<string>(ScrumMasterName, result.ScrumMaster.Name);
                Assert.AreEqual<string>(typeof(D.ScrumMaster).Name, result.ScrumMaster.Type);
            }
        }

        [TestMethod]
        public void CreateTeam_TeamNameAndScrumMasterName_ReturnsTeamWithAvilableEstimations()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.CreateScrumTeam(TeamName, ScrumMasterName)).Returns(teamLock.Object).Verifiable();

            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.CreateTeam(TeamName, ScrumMasterName);

                // Verify
                Assert.IsNotNull(result.AvailableEstimations);
                var expectedCollection = new double?[]
                {
                0.0, 0.5, 1.0, 2.0, 3.0, 5.0, 8.0, 13.0, 20.0, 40.0, 100.0, Estimation.PositiveInfinity, null
                };
                CollectionAssert.AreEquivalent(expectedCollection, result.AvailableEstimations.Select(e => e.Value).ToList());
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateTeam_TeamNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.CreateTeam(null, ScrumMasterName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateTeam_ScrumMasterNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.CreateTeam(TeamName, null);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateTeam_TeamNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.CreateTeam(LongTeamName, ScrumMasterName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateTeam_ScrumMasterNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.CreateTeam(TeamName, LongMemberName);
            }
        }

        [TestMethod]
        public void JoinTeam_TeamNameAndMemberNameAsMember_ReturnsTeamJoined()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.JoinTeam(TeamName, MemberName, false);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.AreEqual<string>(TeamName, result.Name);
                Assert.IsNotNull(result.ScrumMaster);
                Assert.AreEqual<string>(ScrumMasterName, result.ScrumMaster.Name);
                Assert.IsNotNull(result.Members);
                var expectedMembers = new string[] { ScrumMasterName, MemberName };
                CollectionAssert.AreEquivalent(expectedMembers, result.Members.Select(m => m.Name).ToList());
                var expectedMemberTypes = new string[] { typeof(D.ScrumMaster).Name, typeof(D.Member).Name };
                CollectionAssert.AreEquivalent(expectedMemberTypes, result.Members.Select(m => m.Type).ToList());
            }
        }

        [TestMethod]
        public void JoinTeam_TeamNameAndMemberNameAsMember_MemberIsAddedToTheTeam()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.JoinTeam(TeamName, MemberName, false);

                // Verify
                var expectedMembers = new string[] { ScrumMasterName, MemberName };
                CollectionAssert.AreEquivalent(expectedMembers, team.Members.Select(m => m.Name).ToList());
            }
        }

        [TestMethod]
        public void JoinTeam_TeamNameAndMemberNameAsMemberAndEstimationStarted_ScrumMasterIsEstimationParticipant()
        {
            // Arrange
            var team = CreateBasicTeam();
            team.ScrumMaster.StartEstimation();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.JoinTeam(TeamName, MemberName, false);

                // Verify
                var expectedParticipants = new string[] { ScrumMasterName };
                CollectionAssert.AreEquivalent(expectedParticipants, team.EstimationParticipants.Select(m => m.MemberName).ToList());
            }
        }

        [TestMethod]
        public void JoinTeam_TeamNameAndObserverNameAsObserver_ReturnsTeamJoined()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.JoinTeam(TeamName, ObserverName, true);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.AreEqual<string>(TeamName, result.Name);
                Assert.IsNotNull(result.ScrumMaster);
                Assert.AreEqual<string>(ScrumMasterName, result.ScrumMaster.Name);
                Assert.IsNotNull(result.Observers);
                var expectedObservers = new string[] { ObserverName };
                CollectionAssert.AreEquivalent(expectedObservers, result.Observers.Select(m => m.Name).ToList());
                var expectedMemberTypes = new string[] { typeof(D.Observer).Name };
                CollectionAssert.AreEquivalent(expectedMemberTypes, result.Observers.Select(m => m.Type).ToList());
            }
        }

        [TestMethod]
        public void JoinTeam_TeamNameAndObserverNameAsObserver_ObserverIsAddedToTheTeam()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.JoinTeam(TeamName, ObserverName, true);

                // Verify
                var expectedObservers = new string[] { ObserverName };
                CollectionAssert.AreEquivalent(expectedObservers, team.Observers.Select(m => m.Name).ToList());
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void JoinTeam_TeamNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.JoinTeam(null, MemberName, false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void JoinTeam_MemberNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.JoinTeam(TeamName, null, false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JoinTeam_TeamNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.JoinTeam(LongTeamName, MemberName, false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JoinTeam_MemberNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.JoinTeam(TeamName, LongMemberName, false);
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndScrumMasterName_ReturnsReconnectedTeam()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, ScrumMasterName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.ScrumTeam);
                Assert.AreEqual<long>(0, result.LastMessageId);
                Assert.AreEqual<string>(TeamName, result.ScrumTeam.Name);
                Assert.IsNotNull(result.ScrumTeam.ScrumMaster);
                Assert.AreEqual<string>(ScrumMasterName, result.ScrumTeam.ScrumMaster.Name);
                Assert.IsNotNull(result.ScrumTeam.Members);
                var expectedMembers = new string[] { ScrumMasterName };
                CollectionAssert.AreEquivalent(expectedMembers, result.ScrumTeam.Members.Select(m => m.Name).ToList());
                var expectedMemberTypes = new string[] { typeof(D.ScrumMaster).Name };
                CollectionAssert.AreEquivalent(expectedMemberTypes, result.ScrumTeam.Members.Select(m => m.Type).ToList());
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndMemberName_ReturnsReconnectedTeam()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = team.Join(MemberName, false);
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.ScrumTeam);
                Assert.AreEqual<long>(0, result.LastMessageId);
                Assert.AreEqual<string>(TeamName, result.ScrumTeam.Name);
                Assert.IsNotNull(result.ScrumTeam.ScrumMaster);
                Assert.AreEqual<string>(ScrumMasterName, result.ScrumTeam.ScrumMaster.Name);
                Assert.IsNotNull(result.ScrumTeam.Members);
                var expectedMembers = new string[] { ScrumMasterName, MemberName };
                CollectionAssert.AreEquivalent(expectedMembers, result.ScrumTeam.Members.Select(m => m.Name).ToList());
                var expectedMemberTypes = new string[] { typeof(D.ScrumMaster).Name, typeof(D.Member).Name };
                CollectionAssert.AreEquivalent(expectedMemberTypes, result.ScrumTeam.Members.Select(m => m.Type).ToList());
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndObserverName_ReturnsReconnectedTeam()
        {
            // Arrange
            var team = CreateBasicTeam();
            var observer = team.Join(ObserverName, true);
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, ObserverName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.ScrumTeam);
                Assert.AreEqual<long>(0, result.LastMessageId);
                Assert.AreEqual<string>(TeamName, result.ScrumTeam.Name);
                Assert.IsNotNull(result.ScrumTeam.ScrumMaster);
                Assert.AreEqual<string>(ScrumMasterName, result.ScrumTeam.ScrumMaster.Name);

                Assert.IsNotNull(result.ScrumTeam.Members);
                var expectedMembers = new string[] { ScrumMasterName };
                CollectionAssert.AreEquivalent(expectedMembers, result.ScrumTeam.Members.Select(m => m.Name).ToList());
                var expectedMemberTypes = new string[] { typeof(D.ScrumMaster).Name };
                CollectionAssert.AreEquivalent(expectedMemberTypes, result.ScrumTeam.Members.Select(m => m.Type).ToList());

                Assert.IsNotNull(result.ScrumTeam.Observers);
                var expectedObservers = new string[] { ObserverName };
                CollectionAssert.AreEquivalent(expectedObservers, result.ScrumTeam.Observers.Select(m => m.Name).ToList());
            }
        }

        [TestMethod]
        [ExpectedException(typeof(HubException))]
        public void ReconnectTeam_TeamNameAndNonExistingMemberName_HubException()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.ReconnectTeam(TeamName, MemberName);
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndMemberNameWithMessages_ReturnsLastMessageId()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = team.Join(MemberName, false);
            team.ScrumMaster.StartEstimation();

            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.ScrumTeam);
                Assert.AreEqual<long>(1, result.LastMessageId);
                Assert.IsFalse(member.HasMessage);
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndMemberNameAndEstimationInProgress_NoEstimationResult()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            team.ScrumMaster.StartEstimation();
            member.Estimation = new D.Estimation(1);

            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.ScrumTeam);
                Assert.IsNull(result.ScrumTeam.EstimationResult);
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndMemberNameAndEstimationFinished_EstimationResultIsSet()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            team.ScrumMaster.StartEstimation();
            member.Estimation = new D.Estimation(1);
            team.ScrumMaster.Estimation = new D.Estimation(2);

            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.ScrumTeam);
                Assert.IsNotNull(result.ScrumTeam.EstimationResult);

                var expectedEstimations = new double?[] { 2, 1 };
                CollectionAssert.AreEquivalent(expectedEstimations, result.ScrumTeam.EstimationResult.Select(e => e.Estimation.Value).ToList());
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndMemberNameAndMemberEstimated_ReturnsSelectedEstimation()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            team.ScrumMaster.StartEstimation();
            member.Estimation = new D.Estimation(1);

            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.SelectedEstimation);
                Assert.AreEqual<double?>(1, result.SelectedEstimation.Value);
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndMemberNameAndMemberNotEstimated_NoSelectedEstimation()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            team.ScrumMaster.StartEstimation();

            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNull(result.SelectedEstimation);
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndMemberNameAndEstimationNotStarted_NoSelectedEstimation()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);

            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNull(result.SelectedEstimation);
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndMemberNameAndEstimationStarted_AllMembersAreEstimationParticipants()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            team.ScrumMaster.StartEstimation();

            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.ScrumTeam);
                Assert.IsNotNull(result.ScrumTeam.EstimationParticipants);
                var expectedParticipants = new string[] { ScrumMasterName, MemberName };
                CollectionAssert.AreEqual(expectedParticipants, result.ScrumTeam.EstimationParticipants.Select(p => p.MemberName).ToList());
            }
        }

        [TestMethod]
        public void ReconnectTeam_TeamNameAndMemberNameAndEstimationNotStarted_NoEstimationParticipants()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);

            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                var result = target.ReconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.ScrumTeam);
                Assert.IsNull(result.ScrumTeam.EstimationParticipants);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ReconnectTeam_TeamNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.ReconnectTeam(null, MemberName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ReconnectTeam_MemberNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.ReconnectTeam(TeamName, null);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ReconnectTeam_TeamNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.ReconnectTeam(LongTeamName, MemberName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ReconnectTeam_MemberNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.ReconnectTeam(TeamName, LongMemberName);
            }
        }

        [TestMethod]
        public void DisconnectTeam_TeamNameAndScrumMasterName_ScrumMasterIsRemovedFromTheTeam()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.DisconnectTeam(TeamName, ScrumMasterName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNull(team.ScrumMaster);
                Assert.IsFalse(team.Members.Any());
            }
        }

        [TestMethod]
        public void DisconnectTeam_TeamNameAndMemberName_MemberIsRemovedFromTheTeam()
        {
            // Arrange
            var team = CreateBasicTeam();
            team.Join(MemberName, false);
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.DisconnectTeam(TeamName, MemberName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                var expectedMembers = new string[] { ScrumMasterName };
                CollectionAssert.AreEquivalent(expectedMembers, team.Members.Select(m => m.Name).ToList());
            }
        }

        [TestMethod]
        public void DisconnectTeam_TeamNameAndObserverName_ObserverIsRemovedFromTheTeam()
        {
            // Arrange
            var team = CreateBasicTeam();
            team.Join(ObserverName, true);
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.DisconnectTeam(TeamName, ObserverName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsFalse(team.Observers.Any());
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DisconnectTeam_TeamNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.DisconnectTeam(null, MemberName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DisconnectTeam_MemberNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.DisconnectTeam(TeamName, null);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DisconnectTeam_TeamNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.DisconnectTeam(LongTeamName, MemberName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DisconnectTeam_MemberNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.DisconnectTeam(TeamName, LongMemberName);
            }
        }

        [TestMethod]
        public void StartEstimation_TeamName_ScrumTeamEstimationIsInProgress()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.StartEstimation(TeamName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.AreEqual<D.TeamState>(D.TeamState.EstimationInProgress, team.State);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StartEstimation_TeamNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.StartEstimation(null);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void StartEstimation_TeamNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.StartEstimation(LongTeamName);
            }
        }

        [TestMethod]
        public void CancelEstimation_TeamName_ScrumTeamEstimationIsCanceled()
        {
            // Arrange
            var team = CreateBasicTeam();
            team.ScrumMaster.StartEstimation();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.CancelEstimation(TeamName);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.AreEqual<D.TeamState>(D.TeamState.EstimationCanceled, team.State);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CancelEstimation_TeamNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.CancelEstimation(null);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CancelEstimation_TeamNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.CancelEstimation(LongTeamName);
            }
        }

        [TestMethod]
        public void SubmitEstimation_TeamNameAndScrumMasterName_EstimationIsSetForScrumMaster()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(TeamName, ScrumMasterName, 2.0);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(team.ScrumMaster.Estimation);
                Assert.AreEqual<double?>(2.0, team.ScrumMaster.Estimation.Value);
            }
        }

        [TestMethod]
        public void SubmitEstimation_TeamNameAndScrumMasterNameAndNull_EstimationIsSetToNull()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(TeamName, ScrumMasterName, null);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(team.ScrumMaster.Estimation);
                Assert.IsNull(team.ScrumMaster.Estimation.Value);
            }
        }

        [TestMethod]
        public void SubmitEstimation_TeamNameAndMemberNameAndMinus1111100_EstimationOfMemberIsSetToInfinity()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(TeamName, MemberName, -1111100.0);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(member.Estimation);
                Assert.IsTrue(double.IsPositiveInfinity(member.Estimation.Value.Value));
            }
        }

        [TestMethod]
        public void SubmitEstimation_TeamNameAndMemberName_EstimationOfMemberIsSet()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(TeamName, MemberName, 8.0);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(member.Estimation);
                Assert.AreEqual<double?>(8.0, member.Estimation.Value);
            }
        }

        [TestMethod]
        public void SubmitEstimation_TeamNameAndMemberNameAndMinus1111100_EstimationOfMemberIsSetInifinty()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(TeamName, MemberName, -1111100.0);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(member.Estimation);
                Assert.IsTrue(double.IsPositiveInfinity(member.Estimation.Value.Value));
            }
        }

        [TestMethod]
        public void SubmitEstimation_TeamNameAndMemberNameAndNull_EstimationIsSetToNull()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(TeamName, MemberName, null);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                teamLock.Verify(l => l.Team);

                Assert.IsNotNull(member.Estimation);
                Assert.IsNull(member.Estimation.Value);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SubmitEstimation_TeamNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(null, MemberName, 0.0);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SubmitEstimation_MemberNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(TeamName, null, 0.0);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SubmitEstimation_TeamNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(LongTeamName, MemberName, 1.0);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SubmitEstimation_MemberNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.SubmitEstimation(TeamName, LongMemberName, 1.0);
            }
        }

        [TestMethod]
        public void GetMessages_MemberJoinedTeam_ScrumMasterGetsMessage()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = team.Join(MemberName, false);
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            planningPoker.Setup(p => p.GetMessagesAsync(team.ScrumMaster, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => team.ScrumMaster.Messages.ToList()).Verifiable();

            IList<Message> result = null;
            var clientContext = new Mock<IPlanningPokerClient>(MockBehavior.Strict);
            clientContext.Setup(o => o.Notify(It.IsAny<IList<Message>>()))
                .Callback<IList<Message>>(m => result = m)
                .Returns(Task.CompletedTask);

            using (var target = CreatePlanningPokerHub(planningPoker.Object, clientContext.Object))
            {
                // Act
                target.GetMessages(TeamName, ScrumMasterName, 0);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                clientContext.Verify(o => o.Notify(It.IsAny<IList<Message>>()));

                Assert.IsNotNull(result);
                Assert.AreEqual<int>(1, result.Count);
                Assert.AreEqual<long>(1, result[0].Id);
                Assert.AreEqual<MessageType>(MessageType.MemberJoined, result[0].Type);
                Assert.IsInstanceOfType(result[0], typeof(MemberMessage));
                var memberMessage = (MemberMessage)result[0];
                Assert.IsNotNull(memberMessage.Member);
                Assert.AreEqual<string>(MemberName, memberMessage.Member.Name);
            }
        }

        [TestMethod]
        public void GetMessages_EstimationEnded_ScrumMasterGetsMessages()
        {
            // Arrange
            var team = CreateBasicTeam();
            var member = (D.Member)team.Join(MemberName, false);
            var master = team.ScrumMaster;
            master.StartEstimation();
            master.Estimation = new D.Estimation(1.0);
            member.Estimation = new D.Estimation(2.0);

            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            planningPoker.Setup(p => p.GetMessagesAsync(master, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => master.Messages.ToList()).Verifiable();

            IList<Message> result = null;
            var clientContext = new Mock<IPlanningPokerClient>(MockBehavior.Strict);
            clientContext.Setup(o => o.Notify(It.IsAny<IList<Message>>()))
                .Callback<IList<Message>>(m => result = m)
                .Returns(Task.CompletedTask);

            using (var target = CreatePlanningPokerHub(planningPoker.Object, clientContext.Object))
            {
                // Act
                target.GetMessages(TeamName, ScrumMasterName, 1);

                // Verify
                planningPoker.Verify();
                teamLock.Verify();
                clientContext.Verify(o => o.Notify(It.IsAny<IList<Message>>()));

                Assert.IsNotNull(result);
                Assert.AreEqual<int>(4, result.Count);
                Assert.AreEqual<long>(2, result[0].Id);
                Assert.AreEqual<MessageType>(MessageType.EstimationStarted, result[0].Type);

                Assert.AreEqual<long>(3, result[1].Id);
                Assert.AreEqual<MessageType>(MessageType.MemberEstimated, result[1].Type);
                Assert.AreEqual<long>(4, result[2].Id);
                Assert.AreEqual<MessageType>(MessageType.MemberEstimated, result[2].Type);

                Assert.AreEqual<long>(5, result[3].Id);
                Assert.AreEqual<MessageType>(MessageType.EstimationEnded, result[3].Type);
                Assert.IsInstanceOfType(result[3], typeof(EstimationResultMessage));
                var estimationResultMessage = (EstimationResultMessage)result[3];

                Assert.IsNotNull(estimationResultMessage.EstimationResult);
                var expectedResult = new Tuple<string, double>[]
                {
                new Tuple<string, double>(ScrumMasterName, 1.0),
                new Tuple<string, double>(MemberName, 2.0)
                };
                CollectionAssert.AreEquivalent(expectedResult, estimationResultMessage.EstimationResult.Select(i => new Tuple<string, double>(i.Member.Name, i.Estimation.Value.Value)).ToList());
            }
        }

        [TestMethod]
        public void GetMessages_NoMessagesOnTime_ReturnsEmptyCollection()
        {
            // Arrange
            var team = CreateBasicTeam();
            var teamLock = CreateTeamLock(team);
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            planningPoker.Setup(p => p.GetScrumTeam(TeamName)).Returns(teamLock.Object).Verifiable();
            planningPoker.Setup(p => p.GetMessagesAsync(team.ScrumMaster, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<D.Message>()).Verifiable();

            IList<Message> result = null;
            var clientContext = new Mock<IPlanningPokerClient>(MockBehavior.Strict);
            clientContext.Setup(o => o.Notify(It.IsAny<IList<Message>>()))
                .Callback<IList<Message>>(m => result = m)
                .Returns(Task.CompletedTask);

            using (var target = CreatePlanningPokerHub(planningPoker.Object, clientContext.Object))
            {
                // Act
                target.GetMessages(TeamName, ScrumMasterName, 0);

                // Verify
                planningPoker.Verify();
                clientContext.Verify(o => o.Notify(It.IsAny<IList<Message>>()));

                Assert.IsNotNull(result);
                Assert.AreEqual<int>(0, result.Count);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetMessages_TeamNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.GetMessages(null, MemberName, 0);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetMessages_MemberNameIsEmpty_ArgumentNullException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.GetMessages(TeamName, null, 0);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetMessages_TeamNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.GetMessages(LongTeamName, MemberName, 0);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetMessages_MemberNameTooLong_ArgumentException()
        {
            // Arrange
            var planningPoker = new Mock<D.IPlanningPoker>(MockBehavior.Strict);
            using (var target = CreatePlanningPokerHub(planningPoker.Object))
            {
                // Act
                target.GetMessages(TeamName, LongMemberName, 0);
            }
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }

        private static D.ScrumTeam CreateBasicTeam()
        {
            var result = new D.ScrumTeam(TeamName);
            result.SetScrumMaster(ScrumMasterName);
            return result;
        }

        private static Mock<D.IScrumTeamLock> CreateTeamLock(D.ScrumTeam scrumTeam)
        {
            var result = new Mock<D.IScrumTeamLock>(MockBehavior.Strict);
            result.Setup(l => l.Team).Returns(scrumTeam);
            result.Setup(l => l.Lock()).Verifiable();
            result.Setup(l => l.Dispose()).Verifiable();
            return result;
        }

        private PlanningPokerHub CreatePlanningPokerHub(
            D.IPlanningPoker planningPoker,
            IPlanningPokerClient client = null,
            string connectionId = null)
        {
            if (connectionId == null)
            {
                connectionId = Guid.NewGuid().ToString();
            }

            var clientContext = new Mock<IHubContext<PlanningPokerHub, IPlanningPokerClient>>(MockBehavior.Strict);
            if (client != null)
            {
                var clients = new Mock<IHubClients<IPlanningPokerClient>>(MockBehavior.Strict);
                clientContext.SetupGet(o => o.Clients).Returns(clients.Object);
                clients.Setup(o => o.Client(connectionId)).Returns(client);
            }

            var logger = new Logger<PlanningPokerHub>(_loggerFactory);

            var result = new PlanningPokerHub(planningPoker, clientContext.Object, logger);

            var context = new HubCallerContextMock();
            context.SetConnectionId(connectionId);
            result.Context = context;

            return result;
        }
    }
}
