using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class ActionSelector
    {
        private const int NUM_POSSIBLE_MOVES = 2;
        private const int ACCELERATE = 0;
        private const int DEACCELERATE = 1;

        private const int DISCRETIZATION_VELOCITYX = 10;
        private const int DISCRETIZATION_DISTANCEX = 4;

        private const int MAX_DISTANCEX = 200;

        private const int NUM_STATE = 4000;
        private const int NUM_TARGET_VELOCITYX = GameInfo.MAX_VELOCITY_X / (DISCRETIZATION_VELOCITYX * 2);

        private const int NUM_ROW_QMAP = NUM_STATE;
        private const int NUM_COLUMN_QMAP = NUM_POSSIBLE_MOVES * NUM_TARGET_VELOCITYX;

        /// <summary>
        /// "Qmap[filas,columnas]"El numero de filas es son el numero de estados. En esto el numero de columnas son todas las posibles velocidades * los dos posibles movimientos
        /// </summary>
        private float[,] Qmap;

        public ActionSelector()
        {
            Qmap = Utilities.ReadCsvFile(NUM_ROW_QMAP, NUM_COLUMN_QMAP, "Agents\\Qmap.csv");
            
        }

        //
        public bool IsGoal(RectangleRepresentation rectangleRepresentation, int targetPointX, int targetVelocityX, bool rightMove)
        {
            float distanceX = rightMove ? rectangleRepresentation.X - targetPointX : targetPointX - rectangleRepresentation.X;

            if (-DISCRETIZATION_DISTANCEX * 2 < distanceX && distanceX <= 0)
            {
                float relativeVelocityX = rightMove ? rectangleRepresentation.VelocityX : -rectangleRepresentation.VelocityX;

                if (targetVelocityX == 0)
                {
                    if (targetVelocityX - DISCRETIZATION_VELOCITYX <= relativeVelocityX && relativeVelocityX < targetVelocityX + DISCRETIZATION_VELOCITYX)
                    {
                        return true;
                    }
                }
                else
                {
                    if (targetVelocityX <= relativeVelocityX && relativeVelocityX < targetVelocityX + DISCRETIZATION_VELOCITYX * 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Esta función lo que hace es retornar si debe moverse a la izq o derecha tomando en cuenta la posicion del agente y su velocidad y la posicion y velocidad que deseea obtener.  
        /// </summary>
        public Moves GetCurrentAction(RectangleRepresentation rectangleRepresentation, int targetPointX, int targetVelocityX, bool rightMove)
        {
            
            //obtiene el estado del agente según su velocidad y la distancia que tiene a su objetivo.
            int stateNum = GetStateNum(rectangleRepresentation, targetPointX, rightMove);

            int currentActionNum;

            float distanceX = rightMove ? rectangleRepresentation.X - targetPointX : targetPointX - rectangleRepresentation.X;

            //acá se consideran los casos en los que hay mucha o muy poca distancia.
            if (distanceX <= -MAX_DISTANCEX)
            {
                currentActionNum = ACCELERATE;
            }
            else if (distanceX >= MAX_DISTANCEX)
            {
                currentActionNum = DEACCELERATE;
            }
            else
            {
                //JESUUUUS!!!!!
                //Aca se considera la velocidad objetivo
                //Este numero sirve para ver si el agente debe acelerar o frenar.
                //dependiendo del estado actual y de la velocidad que quiere alcanzar.
                currentActionNum = GetOptimalActionNum(stateNum, targetVelocityX);
            }

            Moves currentAction;

            if (currentActionNum == ACCELERATE)
            {
                currentAction = rightMove ? Moves.MOVE_RIGHT : Moves.MOVE_LEFT;
            }
            else
            {
                currentAction = rightMove ? Moves.MOVE_LEFT : Moves.MOVE_RIGHT;
            }

            return currentAction;
        }

      
        public int GetStateNum(RectangleRepresentation rectangleRepresentation, int targetPointX, bool rightMove)
        {
            //divide la velocidad por una constante para que la velocidad este en un rango de enteros.
            int discretizedVelocityX = (int)((rightMove ? rectangleRepresentation.VelocityX : -rectangleRepresentation.VelocityX) + 200) / DISCRETIZATION_VELOCITYX;

            if (discretizedVelocityX < 0)
            {
                discretizedVelocityX = 0;
            }
            else if (GameInfo.MAX_VELOCITY_X * 2 / DISCRETIZATION_VELOCITYX <= discretizedVelocityX)
            {
                discretizedVelocityX = GameInfo.MAX_VELOCITY_X * 2 / DISCRETIZATION_VELOCITYX - 1;
            }

            //distancia hacia el objetivo en enteros
            int discretizedDistanceX = (int)((rightMove ? rectangleRepresentation.X - targetPointX : targetPointX - rectangleRepresentation.X) + MAX_DISTANCEX) / DISCRETIZATION_DISTANCEX;

            if (discretizedDistanceX < 0)
            {
                discretizedDistanceX = 0;
            }
            else if (MAX_DISTANCEX * 2 / DISCRETIZATION_DISTANCEX <= discretizedDistanceX)
            {
                discretizedDistanceX = MAX_DISTANCEX * 2 / DISCRETIZATION_DISTANCEX - 1;
            }

            return discretizedVelocityX + discretizedDistanceX * (GameInfo.MAX_VELOCITY_X * 2 / DISCRETIZATION_VELOCITYX);
        }

        /// <summary>
        /// El numero que esto devuelve es??¡¡¡¡¡¡¡
        /// </summary>
        private int GetOptimalActionNum(int stateNum, int targetVelocityX)
        {
            int maxColumnNum = 0;
            float maxValue = float.MinValue;

            //matematicas lol
            int from = (targetVelocityX / (DISCRETIZATION_VELOCITYX * 2)) * 2;
            int to = from + NUM_POSSIBLE_MOVES;

            for (int i = from; i < to; i++)
            {

                if (maxValue < Qmap[stateNum, i])
                {
                    maxValue = Qmap[stateNum, i];
                    maxColumnNum = i;
                }
            }

            return maxColumnNum - from;
        }
    }
}