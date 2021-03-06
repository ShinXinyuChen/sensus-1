﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Xamarin.Forms;
using SensusService.Probes.Location;
using System.Linq;

namespace SensusUI
{
    public class ProximityTriggersPage : ContentPage
    {
        public ProximityTriggersPage(IPointsOfInterestProximityProbe proximityProbe)
        {
            Title = "Proximity Triggers";

            ListView triggerList = new ListView();
            triggerList.ItemTemplate = new DataTemplate(typeof(TextCell));
            triggerList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            triggerList.ItemsSource = proximityProbe.Triggers;

            Content = triggerList;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
                    {
                        if (UiBoundSensusServiceHelper.Get(true).PointsOfInterest.Union(proximityProbe.Protocol.PointsOfInterest).Count() > 0)
                            await Navigation.PushAsync(new AddProximityTriggerPage(proximityProbe));
                        else
                            UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("You must define points of interest before adding triggers.");
                    }));

            ToolbarItems.Add(new ToolbarItem(null, "minus.png", async () =>
                {
                    if (triggerList.SelectedItem != null && await DisplayAlert("Confirm Delete", "Are you sure you want to delete the selected trigger?", "Yes", "Cancel"))
                    {
                        proximityProbe.Triggers.Remove(triggerList.SelectedItem as PointOfInterestProximityTrigger);
                        UiBoundSensusServiceHelper.Get(true).SaveAsync();
                        triggerList.SelectedItem = null;
                    }
                }));
        }
    }
}