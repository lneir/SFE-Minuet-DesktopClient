﻿//Licensed to the Apache Software Foundation(ASF) under one
//or more contributor license agreements.See the NOTICE file
//distributed with this work for additional information
//regarding copyright ownership.The ASF licenses this file
//to you under the Apache License, Version 2.0 (the
//"License"); you may not use this file except in compliance
//with the License.  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing,
//software distributed under the License is distributed on an
//"AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//KIND, either express or implied.  See the License for the
//specific language governing permissions and limitations
//under the License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Paragon.Plugins.Notifications.ViewModels;

namespace Paragon.Plugins.Notifications.Models
{
    public class NotificationCollection
    {
        private const int MaxNotifications = 5;
        private readonly ObservableCollection<Notification> collection;
        private readonly TaskScheduler taskScheduler;

        public NotificationCollection()
        {
            taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            collection = new ObservableCollection<Notification>();
        }

        public IList<Notification> Items
        {
            get { return collection; }
        }

        public event EventHandler<NotificationRemovedArgs> Removed;

        public void Add(Notification notification)
        {
            RemoveGroup(notification.Group);

            collection.Add(notification);

            if (collection.Count >= MaxNotifications)
            {
                var activeNotifications = ActiveNotifications();

                if (!activeNotifications.Any())
                {
                    var firstNonPersistantNotification = activeNotifications.FirstOrDefault(item => !item.IsPersistent);
                    var firstPersistantNotification = activeNotifications.FirstOrDefault(item => item.IsPersistent);
                    var toRemove = firstNonPersistantNotification ?? firstPersistantNotification;

                    RemoveWithDelay(toRemove, RemovedBy.System);
                }
            }

            if (!notification.IsPersistent)
            {
                TaskUtilities
                    .Delay(TimeSpan.FromSeconds(6))
                    .ContinueWith(() => RemoveWithDelay(notification, RemovedBy.System),
                        taskScheduler);
            }
        }

        public void Clear()
        {
            collection.Clear();
        }

        public void Remove(Notification notification, RemovedBy removedBy)
        {
            var removed = collection.Remove(notification);
            if (removed)
            {
				//DES-11128
                if (collection.Count > 0)
                {
                    OnRemoved(notification, removedBy);
                }
                else //is empty
                {
                    OnRemoved(notification, removedBy, true);
                }
            }
        }

        public void RemoveGroup(string group)
        {
            var items = ActiveNotifications(group).ToArray();

            if (items.Any())
            {
                RemoveWithDelay(items, RemovedBy.System);
            }
        }

        public void RemoveWithDelay(Notification notification, RemovedBy removedBy)
        {
            RemoveWithDelay(notification, item => item.IsRemoving = true, removedBy, 200);
        }

        public void RemoveWithDelay(IEnumerable<Notification> items, RemovedBy removedBy)
        {
            RemoveWithDelay(items, item => item.IsRemoving = true, removedBy, 200);
        }
		
		//DES-11128
        private void OnRemoved(Notification notification, RemovedBy removedBy, Boolean hide)
        {
            var onRemoved = Removed;
            if (onRemoved != null)
            {
                onRemoved(this, new NotificationRemovedArgs { Notification = notification, RemovedBy = removedBy, Hide = hide });
            }
        }

        private void OnRemoved(Notification notification, RemovedBy removedBy)
        {
            var onRemoved = Removed;
            if (onRemoved != null)
            {
                onRemoved(this, new NotificationRemovedArgs { Notification = notification, RemovedBy = removedBy });
            }
        }

        private void RemoveWithDelay(
            Notification notification,
            Action<Notification> onBeforeDelay,
            RemovedBy removedBy,
            int millisecondDelay)
        {
            RemoveWithDelay(new[] {notification}, onBeforeDelay, removedBy, millisecondDelay);
        }

        private void RemoveWithDelay(
            IEnumerable<Notification> items,
            Action<Notification> onBeforeDelay,
            RemovedBy removedBy,
            int millisecondDelay)
        {
            foreach (var item in items)
            {
                onBeforeDelay(item);
            }

            TaskUtilities
                .Delay(millisecondDelay)
                .ContinueWith(() =>
                {
                    foreach (var item in items)
                    {
                        var removed = collection.Remove(item);
                        if (removed)
                        {
                            OnRemoved(item, removedBy);
                        }
                    }
                },
                    taskScheduler);
        }

        private IEnumerable<Notification> ActiveNotifications()
        {
            return collection.Where(item => !item.IsRemoving);
        }

        private IEnumerable<Notification> ActiveNotifications(string group)
        {
            return collection.Where(item => !item.IsRemoving && item.Group == group);
        }
    }
}