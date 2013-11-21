﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Octgn.Library.Networking;
using Skylabs.Lobby;

namespace Octgn.Online.GameService
{
    public class GameManager : IDisposable
    {
        #region Singleton

        internal static GameManager SingletonContext { get; set; }

        private static readonly object GameManagerSingletonLocker = new object();

        public static GameManager Instance
        {
            get
            {
                if (SingletonContext == null)
                {
                    lock (GameManagerSingletonLocker)
                    {
                        if (SingletonContext == null)
                        {
                            SingletonContext = new GameManager();
                        }
                    }
                }
                return SingletonContext;
            }
        }

        #endregion Singleton

        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal GameBroadcastListener GameListener;

        public void Start()
        {
            GameListener = AppConfig.Instance.Test ? new GameBroadcastListener(21235) : new GameBroadcastListener(21236);
            GameListener.StartListening();
        }

        public IEnumerable<HostedGameData> Games
        {
            get
            {
                return GameListener.Games
                    .Select(x => new HostedGameData(x.Id,x.GameGuid,x.GameVersion,x.Port
                        ,x.Name,new User(x.Username + "@of.octgn.net"),x.TimeStarted,x.GameName,x.HasPassword,Ports.ExternalIp,x.Source ))
                    .ToArray();
            }
        }

        public void HostGame(HostGameRequest req, User u)
        {
            var bport = 21236;
            if (AppConfig.Instance.Test)
                bport = 21235;

            var game = new HostedGame(Ports.NextPort, req.GameGuid, req.GameVersion,
                req.GameName, req.Name, req.Password, u, false, true,req.RequestId,bport);

            if (game.StartProcess(true))
            {
                // Try to kill every other game this asshole started before this one.
                var others = GameListener.Games.Where(x => x.Username.Equals(u.UserName, StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();
                foreach (var g in others)
                {
                    g.TryKillGame();
                }
            }
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if(GameListener != null)
                GameListener.Dispose();
        }

        #endregion
    }
}