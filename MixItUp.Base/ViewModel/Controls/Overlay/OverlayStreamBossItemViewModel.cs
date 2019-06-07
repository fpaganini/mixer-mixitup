﻿using Mixer.Base.Util;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Util;
using System.Linq;

namespace MixItUp.Base.ViewModel.Controls.Overlay
{
    public class OverlayStreamBossItemViewModel : OverlayCustomHTMLItemViewModelBase
    {
        public string StartingHealthString
        {
            get { return this.startingHealth.ToString(); }
            set
            {
                this.startingHealth = this.GetPositiveIntFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        protected int startingHealth;

        public string FollowBonusString
        {
            get { return this.followBonus.ToString(); }
            set
            {
                this.followBonus = this.GetPositiveDoubleFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        private double followBonus;

        public string HostBonusString
        {
            get { return this.hostBonus.ToString(); }
            set
            {
                this.hostBonus = this.GetPositiveDoubleFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        private double hostBonus;

        public string SubBonusString
        {
            get { return this.subBonus.ToString(); }
            set
            {
                this.subBonus = this.GetPositiveDoubleFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        private double subBonus;

        public string DonationBonusString
        {
            get { return this.donationBonus.ToString(); }
            set
            {
                this.donationBonus = this.GetPositiveDoubleFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        private double donationBonus;

        public string SparkBonusString
        {
            get { return this.sparkBonus.ToString(); }
            set
            {
                this.sparkBonus = this.GetPositiveDoubleFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        private double sparkBonus;

        public string EmberBonusString
        {
            get { return this.emberBonus.ToString(); }
            set
            {
                this.emberBonus = this.GetPositiveDoubleFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        private double emberBonus;

        public string WidthString
        {
            get { return this.width.ToString(); }
            set
            {
                this.width = this.GetPositiveIntFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        protected int width;

        public string HeightString
        {
            get { return this.height.ToString(); }
            set
            {
                this.height = this.GetPositiveIntFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        protected int height;

        public string Font
        {
            get { return this.font; }
            set
            {
                this.font = value;
                this.NotifyPropertyChanged();
            }
        }
        private string font;

        public string TextColor
        {
            get { return this.textColor; }
            set
            {
                this.textColor = value;
                this.NotifyPropertyChanged();
            }
        }
        private string textColor;

        public string BorderColor
        {
            get { return this.borderColor; }
            set
            {
                this.borderColor = value;
                this.NotifyPropertyChanged();
            }
        }
        private string borderColor;

        public string ProgressColor
        {
            get { return this.progressColor; }
            set
            {
                this.progressColor = value;
                this.NotifyPropertyChanged();
            }
        }
        private string progressColor;

        public string BackgroundColor
        {
            get { return this.backgroundColor; }
            set
            {
                this.backgroundColor = value;
                this.NotifyPropertyChanged();
            }
        }
        private string backgroundColor;

        public string DamageAnimationString
        {
            get { return EnumHelper.GetEnumName(this.damageAnimation); }
            set
            {
                this.damageAnimation = EnumHelper.GetEnumValueFromString<OverlayEffectVisibleAnimationTypeEnum>(value);
                this.NotifyPropertyChanged();
            }
        }
        protected OverlayEffectVisibleAnimationTypeEnum damageAnimation;

        public string NewBossAnimationString
        {
            get { return EnumHelper.GetEnumName(this.newBossAnimation); }
            set
            {
                this.newBossAnimation = EnumHelper.GetEnumValueFromString<OverlayEffectVisibleAnimationTypeEnum>(value);
                this.NotifyPropertyChanged();
            }
        }
        protected OverlayEffectVisibleAnimationTypeEnum newBossAnimation;

        public CustomCommand NewBossCommand
        {
            get { return this.newBossCommand; }
            set
            {
                this.newBossCommand = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("IsNewBossCommandCommandSet");
                this.NotifyPropertyChanged("IsNewBossCommandCommandNotSet");
            }
        }
        private CustomCommand newBossCommand;

        public bool IsNewBossCommandCommandSet { get { return this.NewBossCommand != null; } }
        public bool IsNewBossCommandCommandNotSet { get { return !this.IsNewBossCommandCommandSet; } }

        public OverlayStreamBossItemViewModel()
        {
            this.width = 450;
            this.height = 100;
            this.Font = "Arial";

            this.startingHealth = 5000;
            this.followBonus = 1.0;
            this.hostBonus = 1.0;
            this.subBonus = 10.0;
            this.donationBonus = 1.0;
            this.sparkBonus = 0.01;
            this.emberBonus = 0.1;

            this.HTML = OverlayStreamBoss.HTMLTemplate;
        }

        public OverlayStreamBossItemViewModel(OverlayStreamBoss item)
            : this()
        {
            this.startingHealth = item.StartingHealth;
            this.followBonus = item.FollowBonus;
            this.hostBonus = item.HostBonus;
            this.subBonus = item.SubscriberBonus;
            this.donationBonus = item.DonationBonus;
            this.sparkBonus = item.SparkBonus;
            this.emberBonus = item.EmberBonus;

            this.width = item.Width;
            this.height = item.Height;
            this.Font = item.TextFont;

            this.TextColor = item.TextColor;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsValue(this.TextColor))
            {
                this.TextColor = ColorSchemes.HTMLColorSchemeDictionary.FirstOrDefault(c => c.Value.Equals(this.TextColor)).Key;
            }

            this.BorderColor = item.BorderColor;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsValue(this.BorderColor))
            {
                this.BorderColor = ColorSchemes.HTMLColorSchemeDictionary.FirstOrDefault(c => c.Value.Equals(this.BorderColor)).Key;
            }

            this.ProgressColor = item.ProgressColor;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsValue(this.ProgressColor))
            {
                this.ProgressColor = ColorSchemes.HTMLColorSchemeDictionary.FirstOrDefault(c => c.Value.Equals(this.ProgressColor)).Key;
            }

            this.BackgroundColor = item.BackgroundColor;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsValue(this.BackgroundColor))
            {
                this.BackgroundColor = ColorSchemes.HTMLColorSchemeDictionary.FirstOrDefault(c => c.Value.Equals(this.BackgroundColor)).Key;
            }

            this.damageAnimation = item.DamageAnimation;
            this.newBossAnimation = item.NewBossAnimation;

            this.HTML = item.HTMLText;
        }

        public override OverlayItemBase GetItem()
        {
            if (this.startingHealth > 0 && this.width > 0 && this.height > 0 && !string.IsNullOrEmpty(this.HTML))
            {
                if (ColorSchemes.HTMLColorSchemeDictionary.ContainsKey(this.TextColor))
                {
                    this.TextColor = ColorSchemes.HTMLColorSchemeDictionary[this.TextColor];
                }
                if (ColorSchemes.HTMLColorSchemeDictionary.ContainsKey(this.BackgroundColor))
                {
                    this.BackgroundColor = ColorSchemes.HTMLColorSchemeDictionary[this.BackgroundColor];
                }
                if (ColorSchemes.HTMLColorSchemeDictionary.ContainsKey(this.ProgressColor))
                {
                    this.ProgressColor = ColorSchemes.HTMLColorSchemeDictionary[this.ProgressColor];
                }
                if (ColorSchemes.HTMLColorSchemeDictionary.ContainsKey(this.BackgroundColor))
                {
                    this.BackgroundColor = ColorSchemes.HTMLColorSchemeDictionary[this.BackgroundColor];
                }

                return new OverlayStreamBoss(this.HTML, this.startingHealth, this.width, this.height, this.TextColor, this.Font, this.BorderColor, this.BackgroundColor,
                    this.ProgressColor, this.followBonus, this.hostBonus, this.subBonus, this.donationBonus, this.sparkBonus, this.emberBonus, this.damageAnimation, this.newBossAnimation,
                    this.NewBossCommand);
            }
            return null;
        }
    }
}
