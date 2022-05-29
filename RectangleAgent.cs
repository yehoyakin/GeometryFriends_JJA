using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    /// <summary>
    /// A rectangle agent implementation for the GeometryFriends game that demonstrates simple random action selection.
    /// </summary>
    public class RectangleAgent : AbstractRectangleAgent
    {
        // atributos de Clases para nuestro agente
        private SubgoalAStar subgoalAstar;
        private LevelArray levelArray;
        private PlatformRectangle platform;
        private Platform.MoveInfo? nextMove;
        private Platform.PlatformInfo? currentPlatform;
        private Platform.PlatformInfo? previousPlatform;
        private ActionSelector actionSelector;

        // atributos añadidos a nuestro agente
        private int currentCollectibleNum;
        private int previousCollectibleNum;
        private int targetPointX_InAir;

        private bool getCollectibleFlag;
        private bool differentPlatformFlag;

        //agent implementation specificiation
        private bool implementedAgent;
        private string agentName = "JJA";



        //auxiliary variables for agent action
        private Moves currentAction;
        private List<Moves> possibleMoves;
        private long lastMoveTime;
        private Random rnd;

        //Sensors Information
        private CountInformation numbersInfo;
        private RectangleRepresentation rectangleInfo;
        private CircleRepresentation circleInfo;
        private ObstacleRepresentation[] obstaclesInfo;
        private ObstacleRepresentation[] rectanglePlatformsInfo;
        private ObstacleRepresentation[] circlePlatformsInfo;
        private CollectibleRepresentation[] collectiblesInfo;

        private int nCollectiblesLeft;

        private List<AgentMessage> messages;

        //Area of the game screen
        protected Rectangle area;

        public RectangleAgent()
        {
            //Instancias
            subgoalAstar = new SubgoalAStar();
            levelArray = new LevelArray();
            platform = new PlatformRectangle();
            actionSelector = new ActionSelector();

            previousPlatform = null;
            currentPlatform = null;
            nextMove = null;

            //Change flag if agent is not to be used
            implementedAgent = true;

            lastMoveTime = DateTime.Now.Second;
            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.MOVE_LEFT);
            possibleMoves.Add(Moves.MOVE_RIGHT);
            possibleMoves.Add(Moves.MORPH_UP);
            possibleMoves.Add(Moves.MORPH_DOWN);

            //messages exchange
            messages = new List<AgentMessage>();
        }

        //implements abstract rectangle interface: used to setup the initial information so that the agent has basic knowledge about the level
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nI">numero total de plataformas y diamantes</param>
        /// <param name="rI">estado inicial rectángulo</param>
        /// <param name="cI">estado inicial circulo</param>
        /// <param name="oI">información de obstáculos</param>
        /// <param name="rPI">información de obstáculos (retcángulo)</param>
        /// <param name="cPI">información de obstáculos (círculo)</param>
        /// <param name="colI">información sobre los diamantes</param>
        /// <param name="area">area de juego (pista)</param>
        /// <param name="timeLimit">tiempo límite del nivel</param>
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            numbersInfo = nI;
            nCollectiblesLeft = nI.CollectiblesCount;
            rectangleInfo = rI;
            circleInfo = cI;
            obstaclesInfo = oI;
            rectanglePlatformsInfo = rPI;
            circlePlatformsInfo = cPI;
            collectiblesInfo = colI;
            this.area = area;

            // primero se crea el array del nivel
            levelArray.CreateLevelArray(collectiblesInfo, obstaclesInfo, rectanglePlatformsInfo);
            platform.SetUp(levelArray.GetLevelArray(), levelArray.initialCollectiblesInfo.Length);

            //send a message to the rectangle informing that the circle setup is complete and show how to pass an attachment: a pen object
            messages.Add(new AgentMessage("Setup complete, testing to send an object as an attachment.", new Pen(Color.BlanchedAlmond)));

            //DebugSensorsInfo();
        }

        //implements abstract rectangle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            nCollectiblesLeft = nC;

            rectangleInfo = rI;
            circleInfo = cI;
            collectiblesInfo = colI;
        }

        //implements abstract rectangle interface: signals if the agent is actually implemented or not
        public override bool ImplementedAgent()
        {
            return implementedAgent;
        }

        //implements abstract rectangle interface: provides the name of the agent to the agents manager in GeometryFriends
        public override string AgentName()
        {
            return agentName;
        }

        //simple algorithm for choosing a random action for the rectangle agent
        private void RandomAction()
        {
            /*
             Rectangle Actions
             MOVE_LEFT = 5
             MOVE_RIGHT = 6
             MORPH_UP = 7
             MORPH_DOWN = 8
            */

            currentAction = possibleMoves[rnd.Next(possibleMoves.Count)];

            //send a message to the circle agent telling what action it chose
            messages.Add(new AgentMessage("Going to :" + currentAction));
        }

        //implements abstract rectangle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            return currentAction;
        }

        //implements abstract rectangle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {
            if ((lastMoveTime) <= (DateTime.Now.Second) && (lastMoveTime < 60))
                //(DateTime.Now - lastMoveTime).TotalMilliseconds >= 20)
            {

                //Acá se asigna la plataforma actual "currentPlataform" y la flag "differentPlatformFlag" de que si cambio o no de plataforma.
                IsDifferentPlatform();
                //se asigna una flag "getCollectibleFlag" para ver si se recolecto un diamante.
                IsGetCollectible();

                // rectangle on a platform
                if (currentPlatform.HasValue)
                {
                    //esntra acá cuando termina su movimiento actual.
                    if (differentPlatformFlag || getCollectibleFlag)
                    {
                        differentPlatformFlag = false;
                        getCollectibleFlag = false;

                        targetPointX_InAir = (currentPlatform.Value.leftEdge + currentPlatform.Value.rightEdge) / 2;

                        //Acá se asigna la variable "nextMove" usando A*.
                        Task.Factory.StartNew(SetNextMove);
                    }
                    // movimiento asignado al punto del grafo
                    if (nextMove.HasValue)
                    {
                        //entra acá cuando esta dentro del limite de velocidad en Y
                        if (-GameInfo.MAX_VELOCITY_Y <= rectangleInfo.VelocityY && rectangleInfo.VelocityY <= GameInfo.MAX_VELOCITY_Y)
                        {
                            #region stairGapAction
                            if (nextMove.Value.movementType == Platform.movementType.STAIR_GAP)
                            {
                                if (nextMove.Value.height < 55 && rectangleInfo.Height > 55)
                                {
                                    currentAction = Moves.MORPH_DOWN;
                                }

                                else if (nextMove.Value.height > 190 && rectangleInfo.Height < 190)
                                {
                                    currentAction = Moves.MORPH_UP;
                                }

                                else
                                {
                                    currentAction = nextMove.Value.rightMove ? Moves.MOVE_RIGHT : Moves.MOVE_LEFT;
                                }
                            }
                            #endregion

                            #region ActionByPlatformMoveType

                            else if (nextMove.Value.movementType == Platform.movementType.MORPH_DOWN && rectangleInfo.Height > nextMove.Value.height - LevelArray.PIXEL_LENGTH)
                            {
                                currentAction = Moves.MORPH_DOWN;
                            }

                            else if (nextMove.Value.movementType == Platform.movementType.FALL && nextMove.Value.velocityX == 0 && rectangleInfo.Height > 55)
                            {
                                currentAction = Moves.MORPH_DOWN;
                            }

                            else if (nextMove.Value.movementType == Platform.movementType.MORPH_DOWN && rectangleInfo.Height <= nextMove.Value.height - LevelArray.PIXEL_LENGTH)
                            {
                                currentAction = nextMove.Value.rightMove ? Moves.MOVE_RIGHT : Moves.MOVE_LEFT;
                            }

                            else
                            {
                                currentAction = actionSelector.GetCurrentAction(rectangleInfo, nextMove.Value.movePoint.x, nextMove.Value.velocityX, nextMove.Value.rightMove);
                            }
                            #endregion
                        }
                        else
                        {
                            currentAction = actionSelector.GetCurrentAction(rectangleInfo, targetPointX_InAir, 0, true);
                        }
                    }
                }

                // rectangle is not on a platform
                else
                {
                    if (nextMove.HasValue)
                    {

                        #region stairGapAction
                        if (nextMove.Value.movementType == Platform.movementType.STAIR_GAP)
                        {
                            if (nextMove.Value.height < 55 && rectangleInfo.Height > 55)
                            {
                                currentAction = Moves.MORPH_DOWN;
                            }

                            else if (nextMove.Value.height > 190 && rectangleInfo.Height < 190)
                            {
                                currentAction = Moves.MORPH_UP;
                            }

                            else
                            {
                                currentAction = nextMove.Value.rightMove ? Moves.MOVE_RIGHT : Moves.MOVE_LEFT;
                            }
                        }
                        #endregion
                        
                        else if (nextMove.Value.movementType == Platform.movementType.MORPH_DOWN && rectangleInfo.Height > nextMove.Value.height - LevelArray.PIXEL_LENGTH)
                        {
                            currentAction = Moves.MORPH_DOWN;
                        }

                        else if (nextMove.Value.movementType == Platform.movementType.MORPH_DOWN && rectangleInfo.Height <= nextMove.Value.height - LevelArray.PIXEL_LENGTH)
                        {
                            currentAction = nextMove.Value.rightMove ? Moves.MOVE_RIGHT : Moves.MOVE_LEFT;
                        }

                        else
                        {
                            if (nextMove.Value.collideCeiling && rectangleInfo.VelocityY < 0)
                            {
                                currentAction = Moves.NO_ACTION;
                            }
                            else
                            {
                                //currentAction = actionSelector.GetCurrentAction(rectangleInfo, targetPointX_InAir, 0, true);
                                currentAction = actionSelector.GetCurrentAction(rectangleInfo, nextMove.Value.movePoint.x, nextMove.Value.velocityX, nextMove.Value.rightMove);
                            }
                        }
                    }
                }

                if (!nextMove.HasValue)
                {
                    currentAction = actionSelector.GetCurrentAction(rectangleInfo, (int)rectangleInfo.X, 0, false);
                }

                lastMoveTime = DateTime.Now.Second;
                //DebugSensorsInfo();
            }

            if (nextMove.HasValue)
            {
                if (!actionSelector.IsGoal(rectangleInfo, nextMove.Value.movePoint.x, nextMove.Value.velocityX, nextMove.Value.rightMove))
                {
                    return;
                }

                if (-GameInfo.MAX_VELOCITY_Y <= rectangleInfo.VelocityY && rectangleInfo.VelocityY <= GameInfo.MAX_VELOCITY_Y)
                {
                    targetPointX_InAir = (nextMove.Value.reachablePlatform.leftEdge + nextMove.Value.reachablePlatform.rightEdge) / 2;

                    //if (nextEdge.Value.movementType == Platform.movementType.NO_ACTION && rectangleInfo.Height < nextEdge.Value.height)
                    if (nextMove.Value.movementType == Platform.movementType.NO_ACTION)
                    {
                        if (rectangleInfo.Height < nextMove.Value.height)
                        {
                            currentAction = Moves.MORPH_UP;
                        }
                        else if (rectangleInfo.Height > nextMove.Value.height)
                        {
                            currentAction = Moves.MORPH_DOWN;
                        }

                    }

                    if (nextMove.Value.movementType == Platform.movementType.FALL && nextMove.Value.velocityX == 0 && rectangleInfo.Height < 190)
                    {
                        currentAction = Moves.MORPH_UP;
                    }
                }
            }

            /*
            if (lastMoveTime == 60)
                lastMoveTime = 0;

            if ((lastMoveTime) <= (DateTime.Now.Second) && (lastMoveTime < 60))
            {
                if (!(DateTime.Now.Second == 59))
                {
                    ////////
                    // Actions go here
                    ////////
                    //RandomAction();
                    Console.WriteLine("X: "+rectangleInfo.X);
                    Console.WriteLine("Y: "+rectangleInfo.Y);
                    Console.WriteLine("H: "+rectangleInfo.Height);


                    currentAction = Moves.MOVE_RIGHT;
                    lastMoveTime = lastMoveTime + 1;
                    //DebugSensorsInfo();
                }
                else
                    lastMoveTime = 60;
            }
            */
        }

        //typically used console debugging used in previous implementations of GeometryFriends
        protected void DebugSensorsInfo()
        {
            Log.LogInformation("Rectangle Aagent - " + numbersInfo.ToString());

            Log.LogInformation("Rectangle Aagent - " + rectangleInfo.ToString());

            Log.LogInformation("Rectangle Aagent - " + circleInfo.ToString());

            foreach (ObstacleRepresentation i in obstaclesInfo)
            {
                Log.LogInformation("Rectangle Aagent - " + i.ToString("Obstacle"));
            }

            foreach (ObstacleRepresentation i in rectanglePlatformsInfo)
            {
                Log.LogInformation("Rectangle Aagent - " + i.ToString("Rectangle Platform"));
            }

            foreach (ObstacleRepresentation i in circlePlatformsInfo)
            {
                Log.LogInformation("Rectangle Aagent - " + i.ToString("Circle Platform"));
            }

            foreach (CollectibleRepresentation i in collectiblesInfo)
            {
                Log.LogInformation("Rectangle Aagent - " + i.ToString());
            }
        }

        private void IsGetCollectible()
        {
            if (previousCollectibleNum != currentCollectibleNum)
            {
                getCollectibleFlag = true;
            }

            previousCollectibleNum = currentCollectibleNum;
        }


        private void IsDifferentPlatform()
        {
            currentPlatform = platform.GetPlatform_onRectangle(new LevelArray.Point((int)rectangleInfo.X, (int)rectangleInfo.Y), rectangleInfo.Height);

            if (currentPlatform.HasValue)
            {
                if (!previousPlatform.HasValue)
                {
                    differentPlatformFlag = true;
                }
                else if (currentPlatform.Value.id != previousPlatform.Value.id)
                {
                    differentPlatformFlag = true;
                }
            }

            previousPlatform = currentPlatform;
        }

        /// <summary>
        /// Usa el algoritmo subgoal A* para asignar la variable nextMove
        /// </summary>
        private void SetNextMove()
        {

            nextMove = null;
            nextMove = subgoalAstar.CalculateShortestPath(currentPlatform.Value, new LevelArray.Point((int)rectangleInfo.X, (int)rectangleInfo.Y),
                Enumerable.Repeat<bool>(true, levelArray.initialCollectiblesInfo.Length).ToArray(),
                levelArray.GetObtainedCollectibles(collectiblesInfo), levelArray.initialCollectiblesInfo);
        }

        //implements abstract rectangle interface: signals the agent the end of the current level
        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Log.LogInformation("RECTANGLE - Collectibles caught = " + collectiblesCaught + ", Time elapsed - " + timeElapsed);
        }

        //implememts abstract agent interface: send messages to the circle agent
        public override List<GeometryFriends.AI.Communication.AgentMessage> GetAgentMessages()
        {
            List<AgentMessage> toSent = new List<AgentMessage>(messages);
            messages.Clear();
            return toSent;
        }

        //implememts abstract agent interface: receives messages from the circle agent
        public override void HandleAgentMessages(List<GeometryFriends.AI.Communication.AgentMessage> newMessages)
        {
            foreach (AgentMessage item in newMessages)
            {
                Log.LogInformation("Rectangle: received message from circle: " + item.Message);
                if (item.Attachment != null)
                {
                    Log.LogInformation("Received message has attachment: " + item.Attachment.ToString());
                    if (item.Attachment.GetType() == typeof(Pen))
                    {
                        Log.LogInformation("The attachment is a pen, let's see its color: " + ((Pen)item.Attachment).Color.ToString());
                    }
                }
            }
        }
    }
}