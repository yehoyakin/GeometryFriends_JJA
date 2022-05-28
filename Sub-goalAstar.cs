using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class SubgoalAStar
    {
        private const bool CLOSED_LIST = false;
        private const bool OPEN_LIST = true;

        // El grafo que se recorre esta compuesto por structs State
        // cada estado tiene un historial de movimientos, para cambiar de plataforma u obtener un diamante
        // se recorre el grafo con A* utlizando distancia euclidiana, la ubicación de cada estado/plataforma se establece
        // con el punto en que se encuentra la plataforma, este se toma desde el levelArray 
        public struct State
        {
            public Platform.PlatformInfo currentPlatform;
            public LevelArray.Point currentPoint;
            public bool[] obtainedCollectibles;
            public int numObtainedCollectibles;
            public int totalCost;
            public List<Platform.MoveInfo> moveHistory;

            public State(Platform.PlatformInfo currentPlatform, LevelArray.Point currentPoint, bool[] obtainedCollectibles, int numObtainedCollectibles, int totalCost, List<Platform.MoveInfo> moveHistory)
            {
                this.currentPlatform = currentPlatform;
                this.currentPoint = currentPoint;
                this.obtainedCollectibles = obtainedCollectibles;
                this.numObtainedCollectibles = numObtainedCollectibles;
                this.totalCost = totalCost;
                this.moveHistory = moveHistory;
            }
        }

        private Stopwatch sw;

        public SubgoalAStar()
        {
            sw = new Stopwatch();
        }

        public Platform.MoveInfo? CalculateShortestPath(Platform.PlatformInfo currentPlatform, LevelArray.Point currentPoint, bool[] goalCollectibles, bool[] obtainedCollectibles, CollectibleRepresentation[] initialCollectibles)
        {
            sw.Restart();

            List<State> openList = new List<State>();
            List<State> closedList = new List<State>();
            openList.Add(new State(currentPlatform, currentPoint, obtainedCollectibles, 0, 0, new List<Platform.MoveInfo>()));
            bool[] reachableCollectibles = new bool[initialCollectibles.Length];
            int numReachableCollectibles = 0;

            State minCostState = new State();
            List<State> connectedStates;

            while (openList.Count != 0)
            {
                // al no encontrar un path rápido
                if (sw.ElapsedMilliseconds >= 500)
                {
                    sw.Stop();
                    // busca un path sin tomar en cuenta los collecionables ubicados más abajos con DeleteLowestCollectiables
                    return CalculateShortestPath(currentPlatform, currentPoint, DeleteLowestCollectibles(goalCollectibles, initialCollectibles), obtainedCollectibles, initialCollectibles);
                }

                minCostState = GetMinCostState(openList);

                openList.Add(minCostState);
                closedList.Remove(minCostState);

                if (IsGoalState(minCostState, goalCollectibles))
                {
                    if (minCostState.moveHistory.Count > 0)
                    {
                        return minCostState.moveHistory[0];
                    }
                    else
                    {
                        return null;
                    }
                }

                connectedStates = GetConnectedStates(minCostState, ref reachableCollectibles, ref numReachableCollectibles);

                foreach (State i in connectedStates)
                {
                    SetLessCostState(i, ref openList, ref closedList, goalCollectibles);
                }
            }

            sw.Stop();
            return CalculateShortestPath(currentPlatform, currentPoint, reachableCollectibles, obtainedCollectibles, initialCollectibles);
        }

        private State GetMinCostState(List<State> targetList)
        {
            State minState = new State();
            float min = float.MaxValue;

            foreach (State i in targetList)
            {
                if (min > i.totalCost)
                {
                    minState = i;
                    min = i.totalCost;
                }
            }

            return minState;
        }

        private bool IsGoalState(State targetState, bool[] goalCollectibles)
        {
            for (int i = 0; i < goalCollectibles.Length; i++)
            {
                if (!targetState.obtainedCollectibles[i] && goalCollectibles[i])
                {
                    return false;
                }
            }

            return true;
        }

        private List<State> GetConnectedStates(State targetState, ref bool[] reachableCollectibles, ref int numReachableCollectibles)
        {
            List<State> connectedStates = new List<State>();


            int totalCost;
            List<Platform.MoveInfo> moveHistory;

            foreach (Platform.MoveInfo platformMoveInfo in targetState.currentPlatform.moveInfoList)
            {
                bool[] obtainedCollectibles = new bool[targetState.obtainedCollectibles.Length];
                int numObtainedCollectibles = targetState.numObtainedCollectibles;

                for (int j = 0; j < obtainedCollectibles.Length; j++)
                {
                    obtainedCollectibles[j] = targetState.obtainedCollectibles[j] || platformMoveInfo.collectibles_onPath[j];
                    reachableCollectibles[j] = reachableCollectibles[j] || platformMoveInfo.collectibles_onPath[j];

                    if (!targetState.obtainedCollectibles[j] && platformMoveInfo.collectibles_onPath[j])
                    {
                        numObtainedCollectibles++;
                    }
                }

                if (numObtainedCollectibles > numReachableCollectibles)
                {
                    obtainedCollectibles.CopyTo(reachableCollectibles, 0);
                }

                // mejorable podría tomar en cuenta los collecionables más cercano
                totalCost = targetState.totalCost + CalculateDistance(targetState.currentPoint, platformMoveInfo.movePoint) + platformMoveInfo.pathLength;
                moveHistory = new List<Platform.MoveInfo>(targetState.moveHistory);
                moveHistory.Add(platformMoveInfo);

                connectedStates.Add(new State(platformMoveInfo.reachablePlatform, platformMoveInfo.landPoint, obtainedCollectibles, numObtainedCollectibles, totalCost, moveHistory));
            }

            return connectedStates;
        }

        private int CalculateDistance(LevelArray.Point p1, LevelArray.Point p2)
        {
            return (int)Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        private void SetLessCostState(State targetState, ref List<State> openList, ref List<State> closedList, bool[] goalCollectibles)
        {
            State sameState = new State();
            bool sameState_SetFlag = false;
            bool sameState_Location = CLOSED_LIST;

            foreach (State i in openList)
            {
                if (IsSameState(targetState, i, goalCollectibles))
                {
                    sameState_Location = OPEN_LIST;
                    sameState_SetFlag = true;
                    sameState = i;
                    break;
                }
            }

            if (!sameState_SetFlag)
            {
                foreach (State i in closedList)
                {
                    if (IsSameState(targetState, i, goalCollectibles))
                    {
                        sameState_Location = CLOSED_LIST;
                        sameState_SetFlag = true;
                        sameState = i;
                        break;
                    }
                }
            }

            if (sameState_SetFlag)
            {
                if (sameState.totalCost > targetState.totalCost)
                {
                    openList.Add(targetState);

                    if (sameState_Location)
                    {
                        openList.Remove(sameState);
                    }
                    else
                    {
                        closedList.Remove(sameState);
                    }
                }
            }
            else
            {
                openList.Add(targetState);
            }
        }

        private bool IsSameState(State s1, State s2, bool[] goalCollectibles)
        {
            int i;

            if (s1.currentPlatform.id == s2.currentPlatform.id)
            {
                if (!s1.currentPoint.Equals(s2.currentPoint))
                {
                    return false;
                }

                for (i = 0; i < goalCollectibles.Length; i++)
                {
                    if (goalCollectibles[i])
                    {
                        if (s1.obtainedCollectibles[i] ^ s2.obtainedCollectibles[i])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public bool[] DeleteLowestCollectibles(bool[] goalCollectibles, CollectibleRepresentation[] initialCollectibles)
        {
            // mejorable -> Otra metrica
            // buscar el colleccionable más lejano en vez del que esté más abajo.
            float lowestHeight = float.MinValue;
            int lowestCollectibleID = 0;
            bool[] deletedCollectibles = new bool[goalCollectibles.Length];
            goalCollectibles.CopyTo(deletedCollectibles, 0);

            for (int i = 0; i < goalCollectibles.Length; i++)
            {
                if (goalCollectibles[i])
                {
                    if (lowestHeight < initialCollectibles[i].Y)
                    {
                        lowestHeight = initialCollectibles[i].Y;
                        lowestCollectibleID = i;
                    }
                }
            }

            deletedCollectibles[lowestCollectibleID] = false;

            return deletedCollectibles;
        }
    }
}
