﻿using Mixer.Base.Model.Interactive;
using Mixer.Base.Model.User;
using MixItUp.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.User
{
    public class UserContainerViewModel
    {
        private Dictionary<uint, UserViewModel> users = new Dictionary<uint, UserViewModel>();

        private SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public UserContainerViewModel() { }

        public async Task<UserViewModel> GetUser(uint userID)
        {
            return await this.LockWrapper(() =>
            {
                if (this.users.ContainsKey(userID))
                {
                    return Task.FromResult(this.users[userID]);
                }
                return Task.FromResult<UserViewModel>(null);
            });
        }

        public async Task<UserViewModel> GetUser(string interactiveParticipantID)
        {
            return await this.LockWrapper(() =>
            {
                return Task.FromResult(this.users.Values.FirstOrDefault(u => interactiveParticipantID.Equals(u.InteractiveID)));
            });
        }

        public async Task<UserViewModel> AddOrUpdateUser(ChatUserModel chatUser)
        {
            if (chatUser.userId.HasValue)
            {
                await this.LockWrapper(async () =>
                {
                    if (!this.users.ContainsKey(chatUser.userId.GetValueOrDefault()))
                    {
                        this.users[chatUser.userId.GetValueOrDefault()] = new UserViewModel(chatUser);
                        await this.users[chatUser.userId.GetValueOrDefault()].RefreshDetails();
                    }
                    this.users[chatUser.userId.GetValueOrDefault()].SetChatDetails(chatUser);
                });
                return await this.GetUser(chatUser.userId.GetValueOrDefault());
            }
            return null;
        }

        public async Task<UserViewModel> AddOrUpdateUser(InteractiveParticipantModel interactiveUser)
        {
            await this.LockWrapper(async () =>
            {
                if (!this.users.ContainsKey(interactiveUser.userID))
                {
                    this.users[interactiveUser.userID] = new UserViewModel(interactiveUser);
                    await this.users[interactiveUser.userID].RefreshDetails();
                }
                this.users[interactiveUser.userID].SetInteractiveDetails(interactiveUser);
            });
            return await this.GetUser(interactiveUser.userID);
        }

        public async Task RemoveInteractiveUser(InteractiveParticipantModel interactiveUser)
        {
            await this.LockWrapper(() =>
            {
                if (this.users.ContainsKey(interactiveUser.userID))
                {
                    UserViewModel user = this.users[interactiveUser.userID];
                    user.RemoveInteractiveDetails();
                }
                return Task.FromResult(0);
            });
        }

        public async Task<UserViewModel> RemoveUser(uint userID)
        {
            return await this.LockWrapper(() =>
            {
                UserViewModel user = null;
                if (this.users.ContainsKey(userID))
                {
                    user = this.users[userID];
                    this.users.Remove(userID);
                }
                return Task.FromResult(user);
            });
        }

        public async Task<IEnumerable<UserViewModel>> GetAllUsers()
        {
            return await this.LockWrapper(() => Task.FromResult(this.users.Values));
        }

        public async Task<int> Count()
        {
            return await this.LockWrapper(() => Task.FromResult(this.users.Count));
        }

        private async Task LockWrapper(Func<Task> function)
        {
            try
            {
                await this.semaphore.WaitAsync();

                await function();
            }
            catch (Exception ex) { Logger.Log(ex); }
            finally { this.semaphore.Release(); }
        }

        private async Task<T> LockWrapper<T>(Func<Task<T>> function)
        {
            try
            {
                await this.semaphore.WaitAsync();

                return await function();
            }
            catch (Exception ex) { Logger.Log(ex); }
            finally { this.semaphore.Release(); }
            return default(T);
        }
    }
}