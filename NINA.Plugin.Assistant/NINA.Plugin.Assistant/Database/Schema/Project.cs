﻿using Assistant.NINAPlugin.Plan.Scoring.Rules;
using Assistant.NINAPlugin.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public enum ProjectState {
        Draft, Active, Inactive, Closed
    }

    public enum ProjectPriority {
        Low, Normal, High
    }

    public class Project : INotifyPropertyChanged {
        public const int FLATS_HANDLING_OFF = 0;
        public const int FLATS_HANDLING_TARGET_COMPLETION = 100;
        public const int FLATS_HANDLING_IMMEDIATE = 200;

        [Key] public int Id { get; set; }
        [Required] public string ProfileId { get; set; }
        [Required] public string name { get; set; }
        public string description { get; set; }
        public int state_col { get; set; }
        public int priority_col { get; set; }
        public long createDate { get; set; }
        public long? activeDate { get; set; }
        public long? inactiveDate { get; set; }
        public int isMosaic { get; set; }
        public int flatsHandling { get; set; }

        public int minimumTime { get; set; }
        public double minimumAltitude { get; set; }
        public int useCustomHorizon { get; set; }
        public double horizonOffset { get; set; }
        public int meridianWindow { get; set; }
        public int filterSwitchFrequency { get; set; }
        public int ditherEvery { get; set; }
        public int enableGrader { get; set; }
        public double overheadObstructionRadius { get; set; }

        public virtual List<RuleWeight> ruleWeights { get; set; }
        public virtual List<Target> Targets { get; set; }

        public Project() {
        }

        public Project(string profileId) {
            ProfileId = profileId;
            State = ProjectState.Draft;
            Priority = ProjectPriority.Normal;
            CreateDate = DateTime.Now;

            MinimumTime = 30;
            MinimumAltitude = 0;
            UseCustomHorizon = false;
            HorizonOffset = 0;
            OverheadObstructionRadius = 0;
            MeridianWindow = 0;
            FilterSwitchFrequency = 0;
            DitherEvery = 0;
            EnableGrader = true;
            IsMosaic = false;
            FlatsHandling = FLATS_HANDLING_OFF;

            ruleWeights = GetDefaultRuleWeights();
            Targets = new List<Target>();
        }

        [NotMapped]
        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        [NotMapped]
        public string Description {
            get => description;
            set {
                description = value;
                RaisePropertyChanged(nameof(Description));
            }
        }

        [NotMapped]
        public ProjectPriority Priority {
            get { return (ProjectPriority)priority_col; }
            set {
                priority_col = (int)value;
                RaisePropertyChanged(nameof(ProjectPriority));
            }
        }

        [NotMapped]
        public ProjectState State {
            get { return (ProjectState)state_col; }
            set {
                state_col = (int)value;
                RaisePropertyChanged(nameof(ProjectState));
            }
        }

        [NotMapped]
        public DateTime CreateDate {
            get { return SchedulerDatabaseContext.UnixSecondsToDateTime(createDate); }
            set {
                createDate = SchedulerDatabaseContext.DateTimeToUnixSeconds(value);
                RaisePropertyChanged(nameof(CreateDate));
            }
        }

        [NotMapped]
        public DateTime? ActiveDate {
            get { return SchedulerDatabaseContext.UnixSecondsToDateTime(activeDate); }
            set {
                activeDate = SchedulerDatabaseContext.DateTimeToUnixSeconds(value);
                RaisePropertyChanged(nameof(ActiveDate));
            }
        }

        [NotMapped]
        public DateTime? InactiveDate {
            get { return SchedulerDatabaseContext.UnixSecondsToDateTime(inactiveDate); }
            set {
                inactiveDate = SchedulerDatabaseContext.DateTimeToUnixSeconds(value);
                RaisePropertyChanged(nameof(InactiveDate));
            }
        }

        [NotMapped]
        public bool IsMosaic {
            get { return isMosaic == 1; }
            set {
                isMosaic = value ? 1 : 0;
                RaisePropertyChanged(nameof(IsMosaic));
            }
        }

        [NotMapped]
        public int FlatsHandling {
            get { return flatsHandling; }
            set {
                flatsHandling = value;
                RaisePropertyChanged(nameof(FlatsHandling));
            }
        }

        [NotMapped]
        public bool ActiveNow {
            get {
                return State == ProjectState.Active;
            }
        }

        [NotMapped]
        public int MinimumTime {
            get => minimumTime;
            set {
                minimumTime = value;
                RaisePropertyChanged(nameof(MinimumTime));
            }
        }

        [NotMapped]
        public double MinimumAltitude {
            get => minimumAltitude;
            set {
                minimumAltitude = value;
                RaisePropertyChanged(nameof(MinimumAltitude));
            }
        }

        [NotMapped]
        public bool UseCustomHorizon {
            get { return useCustomHorizon == 1; }
            set {
                useCustomHorizon = value ? 1 : 0;
                RaisePropertyChanged(nameof(UseCustomHorizon));
            }
        }

        [NotMapped]
        public double HorizonOffset {
            get => horizonOffset;
            set {
                horizonOffset = value;
                RaisePropertyChanged(nameof(HorizonOffset));
            }
        }

        [NotMapped]
        public int MeridianWindow {
            get => meridianWindow;
            set {
                meridianWindow = value;
                RaisePropertyChanged(nameof(MeridianWindow));
            }
        }

        [NotMapped]
        public int FilterSwitchFrequency {
            get => filterSwitchFrequency;
            set {
                filterSwitchFrequency = value;
                RaisePropertyChanged(nameof(FilterSwitchFrequency));
            }
        }

        [NotMapped]
        public int DitherEvery {
            get => ditherEvery;
            set {
                ditherEvery = value;
                RaisePropertyChanged(nameof(DitherEvery));
            }
        }

        [NotMapped]
        public bool EnableGrader {
            get { return enableGrader == 1; }
            set {
                enableGrader = value ? 1 : 0;
                RaisePropertyChanged(nameof(EnableGrader));
            }
        }

        [NotMapped]
        public List<RuleWeight> RuleWeights {
            get => ruleWeights;
            set {
                ruleWeights = value;
                RaisePropertyChanged(nameof(RuleWeights));
            }
        }

        [NotMapped]
        public double PercentComplete {
            get {
                double totalDesired = 0;
                double totalAccepted = 0;
                foreach (Target target in Targets) {
                    foreach (ExposurePlan plan in target.ExposurePlans) {
                        totalDesired += plan.Desired;
                        totalAccepted += plan.Accepted;
                    }
                }

                return totalDesired == 0 ? 0 : (totalAccepted / totalDesired) * 100;
            }
        }

        [NotMapped]
        public double OverheadObstructionRadius
        {
            get => overheadObstructionRadius;
            set
            {
                overheadObstructionRadius = value;
                RaisePropertyChanged(nameof(OverheadObstructionRadius));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Project GetPasteCopy(string newProfileId) {
            Project project = new Project();

            project.ProfileId = newProfileId;
            project.name = Utils.CopiedItemName(name);
            project.description = description;
            project.state_col = state_col;
            project.priority_col = priority_col;
            project.createDate = createDate;
            project.activeDate = activeDate;
            project.inactiveDate = inactiveDate;
            project.minimumTime = minimumTime;
            project.minimumAltitude = minimumAltitude;
            project.useCustomHorizon = useCustomHorizon;
            project.horizonOffset = horizonOffset;
            project.meridianWindow = meridianWindow;
            project.filterSwitchFrequency = filterSwitchFrequency;
            project.ditherEvery = ditherEvery;
            project.enableGrader = enableGrader;
            project.isMosaic = isMosaic;
            project.flatsHandling = flatsHandling;
            project.overheadObstructionRadius = overheadObstructionRadius;

            project.Targets = new List<Target>(Targets.Count);
            Targets.ForEach(item => project.Targets.Add(item.GetPasteCopy(newProfileId)));

            project.ruleWeights = new List<RuleWeight>(ruleWeights.Count);
            ruleWeights.ForEach(item => project.ruleWeights.Add(item.GetPasteCopy()));

            return project;
        }

        private List<RuleWeight> GetDefaultRuleWeights() {
            Dictionary<string, IScoringRule> rules = ScoringRule.GetAllScoringRules();
            List<RuleWeight> ruleWeights = new List<RuleWeight>(rules.Count);
            foreach (KeyValuePair<string, IScoringRule> entry in rules) {
                var rule = entry.Value;
                ruleWeights.Add(new RuleWeight(rule.Name, rule.DefaultWeight));
            }

            return ruleWeights;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Description: {Description}");
            sb.AppendLine($"State: {State}");
            sb.AppendLine($"Priority: {Priority}");
            sb.AppendLine($"CreateDate: {CreateDate}");
            sb.AppendLine($"ActiveDate: {ActiveDate}");
            sb.AppendLine($"InactiveDate: {InactiveDate}");
            sb.AppendLine($"MinimumTime: {MinimumTime}");
            sb.AppendLine($"MinimumAltitude: {MinimumAltitude}");
            sb.AppendLine($"UseCustomHorizon: {UseCustomHorizon}");
            sb.AppendLine($"HorizonOffset: {HorizonOffset}");
            sb.AppendLine($"OverheadObstructionRadius: {OverheadObstructionRadius}");
            sb.AppendLine($"MeridianWindow: {MeridianWindow}");
            sb.AppendLine($"FilterSwitchFrequency: {FilterSwitchFrequency}");
            sb.AppendLine($"DitherEvery: {DitherEvery}");
            sb.AppendLine($"EnableGrader: {EnableGrader}");
            sb.AppendLine($"IsMosaic: {IsMosaic}");
            sb.AppendLine($"FlatsHandling: {FlatsHandling}");
            sb.AppendLine($"RuleWeights:");
            foreach (var item in RuleWeights) {
                sb.AppendLine($"  {item.Name} {item.Weight}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    internal class ProjectConfiguration : EntityTypeConfiguration<Project> {

        public ProjectConfiguration() {
            HasKey(p => new { p.Id });
            HasMany(p => p.Targets)
                .WithRequired(e => e.Project)
                .HasForeignKey(e => e.ProjectId);
            HasMany(p => p.ruleWeights)
                .WithRequired(r => r.Project)
                .HasForeignKey(r => r.ProjectId);
            Property(p => p.state_col).HasColumnName("state");
            Property(p => p.priority_col).HasColumnName("priority");
        }
    }
}