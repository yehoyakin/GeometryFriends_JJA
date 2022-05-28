using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    public class LevelArray
    {
        public const int OBSTACLE = -1;
        public const int OPEN = 0;

        public const int PIXEL_LENGTH = 8;
        // este array sirve para pintar la forma rombo que tienen los diamantes 
        private int[] COLLECTIBLE_SIZE = new int[] { 2, 4, 6, 6, 4, 2 };

        public int[,] levelArray = new int[GameInfo.LEVEL_HEIGHT / PIXEL_LENGTH, GameInfo.LEVEL_WIDTH / PIXEL_LENGTH];

        public CollectibleRepresentation[] initialCollectiblesInfo;

        public struct ArrayPoint
        {
            public int xArrayPoint;
            public int yArrayPoint;

            public ArrayPoint(int xArray, int yArray)
            {
                this.xArrayPoint = xArray;
                this.yArrayPoint = yArray;
            }
        }

        public struct Point
        {
            public int x;
            public int y;

            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }


        public static ArrayPoint ConvertPointIntoArrayPoint(Point value, bool xNegative, bool yNegative)
        {
            return new ArrayPoint(ConvertValue_PointIntoArrayPoint(value.x, xNegative), ConvertValue_PointIntoArrayPoint(value.y, yNegative));
        }

        public static Point ConvertArrayPointIntoPoint(ArrayPoint value)
        {
            return new Point(ConvertValue_ArrayPointIntoPoint(value.xArrayPoint), ConvertValue_ArrayPointIntoPoint(value.yArrayPoint));
        }

        public static int ConvertValue_PointIntoArrayPoint(int pointValue, bool negative)
        {
            int arrayValue = (pointValue - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;

            if (arrayValue < 0)
            {
                arrayValue = 0;
            }

            if (negative)
            {
                if ((pointValue - GameInfo.LEVEL_ORIGINAL) % PIXEL_LENGTH == 0)
                {
                    arrayValue--;
                }
            }

            return arrayValue;
        }

        public static int ConvertValue_ArrayPointIntoPoint(int arrayValue)
        {
            return arrayValue * PIXEL_LENGTH + GameInfo.LEVEL_ORIGINAL;
        }

        public int[,] GetLevelArray()
        {
            return levelArray;
        }

        public void CreateLevelArray(CollectibleRepresentation[] collectiblesInfo, ObstacleRepresentation[] obstaclesInfo, ObstacleRepresentation[] rectanglePlatformsInfo)
        {
            SetCollectibles(collectiblesInfo);

            SetDefaultObstacles();

            SetObstacles(obstaclesInfo);

            SetObstacles(rectanglePlatformsInfo);
        }

        private void SetDefaultObstacles()
        {
            // la funcion llena los obstaculos estándar, el borde de la pista 
            // para esto utiliza el largo del array 0 -> height y largo de array 1 -> width

            for (int i = 0; i <= 3; i++)
            {
                for (int j = 0; j < levelArray.GetLength(1); j++)
                {
                    levelArray[i, j] = OBSTACLE;
                }
            }

            for (int i = 0; i < levelArray.GetLength(0); i++)
            {
                for (int j = 0; j <= 3; j++)
                {
                    levelArray[i, j] = OBSTACLE;
                }
            }

            for (int i = 0; i < levelArray.GetLength(0); i++)
            {
                for (int j = 154; j < levelArray.GetLength(1); j++)
                {
                    levelArray[i, j] = OBSTACLE;
                }
            }

            for (int i = 94; i < levelArray.GetLength(0); i++)
            {
                for (int j = 0; j < levelArray.GetLength(1); j++)
                {
                    levelArray[i, j] = OBSTACLE;
                }
            }
        }

        private void SetObstacles(ObstacleRepresentation[] oI)
        {
            foreach (ObstacleRepresentation obstacle in oI)
            {
                int xPosArray = (int)(obstacle.X - (obstacle.Width / 2) - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;
                int yPosArray = (int)(obstacle.Y - (obstacle.Height / 2) - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;
                int height = (int)(obstacle.Height / PIXEL_LENGTH);
                int width = (int)(obstacle.Width / PIXEL_LENGTH);

                //Con los datos de cada obstáculo y su altura más su ancho, se llena en el array del nivel como obstáculo
                for (int i = yPosArray; i < (yPosArray + height); i++)
                {
                    for (int j = xPosArray; j < (xPosArray + width); j++)
                    {
                        if (0 <= i && i < levelArray.GetLength(0) && 0 <= j && j < levelArray.GetLength(1))
                        {
                            levelArray[i, j] = OBSTACLE;
                        }
                    }
                }
            }
        }

        private void SetCollectibles(CollectibleRepresentation[] collectiblesInfo)
        {
            initialCollectiblesInfo = collectiblesInfo;

            for (int i = 0; i < collectiblesInfo.Length; i++)
            {
                int xPosArray = (int)(collectiblesInfo[i].X - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;
                int yPosArray = (int)(collectiblesInfo[i].Y - GameInfo.LEVEL_ORIGINAL) / PIXEL_LENGTH;

                // con un ciclo se marcan los espacios ocupados por pixel para formar el diamante (collecionable)
                for (int j = 0; j < COLLECTIBLE_SIZE.Length; j++)
                {
                    for (int k = 0; k < COLLECTIBLE_SIZE[j]; k++)
                    {
                        levelArray[yPosArray + (j - COLLECTIBLE_SIZE.Length / 2), xPosArray + (k - COLLECTIBLE_SIZE[j] / 2)] = i + 1;
                    }
                }
            }
        }

        public bool[] GetObtainedCollectibles(CollectibleRepresentation[] currentCollectiblesInfo)
        {
            bool[] obtainedCollectibles = new bool[initialCollectiblesInfo.Length];

            for (int i = 0; i < obtainedCollectibles.Length; i++)
            {
                obtainedCollectibles[i] = true;
            }

            foreach (CollectibleRepresentation i in currentCollectiblesInfo)
            {
                for (int j = 0; j < initialCollectiblesInfo.Length; j++)
                {
                    if (i.Equals(initialCollectiblesInfo[j]))
                    {
                        obtainedCollectibles[j] = false;
                    }
                }
            }

            return obtainedCollectibles;
        }

    }
}
