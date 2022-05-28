using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    class PlatformRectangle : Platform
    {

        public PlatformRectangle() : base() { }

        public override int[,] IdentifyPlatforms(int[,] levelArray)
        {
            int[,] platformArray = new int[levelArray.GetLength(0), levelArray.GetLength(1)];

            for (int i = 0; i < levelArray.GetLength(0); i++)
            {
                Parallel.For(0, levelArray.GetLength(1), j =>
                {
                    LevelArray.Point rectangleCenter = LevelArray.ConvertArrayPointIntoPoint(new LevelArray.ArrayPoint(j, i));
                    rectangleCenter.y -= GameInfo.SQUARE_HEIGHT / 2;

                    List<LevelArray.ArrayPoint> rectanglePixels = GetRectanglePixels(rectangleCenter, GameInfo.SQUARE_HEIGHT);

                    if (IsObstacle_onPixels(levelArray, rectanglePixels))
                    {

                        rectangleCenter = LevelArray.ConvertArrayPointIntoPoint(new LevelArray.ArrayPoint(j, i));
                        rectangleCenter.y -= GameInfo.HORIZONTAL_RECTANGLE_HEIGHT / 2;
                        //rectangleCenter.y += (GameInfo.SQUARE_HEIGHT - GameInfo.HORIZONTAL_RECTANGLE_HEIGHT) / 2;

                        rectanglePixels = GetRectanglePixels(rectangleCenter, GameInfo.HORIZONTAL_RECTANGLE_HEIGHT);

                        if (IsObstacle_onPixels(levelArray, rectanglePixels))
                        {

                            rectangleCenter = LevelArray.ConvertArrayPointIntoPoint(new LevelArray.ArrayPoint(j, i));
                            rectangleCenter.y -= GameInfo.VERTICAL_RECTANGLE_HEIGHT / 2;
                            //rectangleCenter.y += (GameInfo.SQUARE_HEIGHT - GameInfo.VERTICAL_RECTANGLE_HEIGHT) / 2;

                            rectanglePixels = GetRectanglePixels(rectangleCenter, GameInfo.VERTICAL_RECTANGLE_HEIGHT);

                            if (IsObstacle_onPixels(levelArray, rectanglePixels))
                            {
                                return;
                            }
                        }
                    }

                    // if there is no obstacles but there is one below the rectangle, we add this point as platform
                    if (levelArray[i, j - 1] == LevelArray.OBSTACLE || levelArray[i, j] == LevelArray.OBSTACLE)
                    {
                        platformArray[i, j] = LevelArray.OBSTACLE;
                    }

                });
            }

            return platformArray;

        }

        public override void SetPlatformInfoList(int[,] levelArray)
        {
            int[,] platformArray = IdentifyPlatforms(levelArray);

            Parallel.For(0, levelArray.GetLength(0), i =>
            {
                // platform flag sirve para diferenciar entre plataformas con caída y plataformas conectadas a otras
                bool platformFlag = false, platformWithObstacle = false;
                int height = 0, leftEdge = 0, rightEdge = 0, obstacle_height = NO_OBSTACLE;

                // platformArray.GetLength(0) -> altura --> i
                // platformArray.GetLength(1) -> ancho --> j
                for (int j = 0; j < platformArray.GetLength(1); j++)
                {
                    if (platformArray[i, j] == LevelArray.OBSTACLE && !platformFlag)
                    {
                        height = LevelArray.ConvertValue_ArrayPointIntoPoint(i);
                        // la esquina izquierda es donde comienza el array
                        leftEdge = LevelArray.ConvertValue_ArrayPointIntoPoint(j);
                        platformFlag = true;
                    }

                    if (platformArray[i, j] == LevelArray.OPEN && platformFlag)
                    {
                        // la esquina derecha es el final del array menos 1 pixel
                        rightEdge = LevelArray.ConvertValue_ArrayPointIntoPoint(j - 1);

                        if (rightEdge >= leftEdge)
                        {
                            lock (platformInfoList)
                            {
                                // crea la plataforma y la añade a la lista, sabiendo la ubicacion de los bordes izquierda y derecha
                                // en conjunto con la altura basta para saber las coordenadas en pixeles de las 4 esquinas de cada plataforma 
                                platformInfoList.Add(new PlatformInfo(0, height, leftEdge, rightEdge, new List<MoveInfo>(), obstacle_height * LevelArray.PIXEL_LENGTH));
                            }
                        }

                        platformWithObstacle = false;
                        obstacle_height = NO_OBSTACLE;
                        platformFlag = false;
                    }

                    // si esta contigua a otra plataforma
                    // comienza a registrar la plataforma adyacente
                    if (platformFlag && i >= 12)
                    {
                        for (int h = 7; h <= 25; h++)
                        {
                            if (levelArray[i - h, j] == LevelArray.OBSTACLE)
                            {

                                // if we still have a platform with obstacle of the same height
                                if (h == obstacle_height && platformWithObstacle)
                                {
                                    break;
                                }

                                // if we have platform with no obstacle or the obstacle height has changed
                                if (!platformWithObstacle || (platformWithObstacle && h != obstacle_height))
                                {

                                    rightEdge = LevelArray.ConvertValue_ArrayPointIntoPoint(j - 1);

                                    if (rightEdge >= leftEdge)
                                    {
                                        lock (platformInfoList)
                                        {
                                            platformInfoList.Add(new PlatformInfo(0, height, leftEdge, rightEdge, new List<MoveInfo>(), obstacle_height * LevelArray.PIXEL_LENGTH));
                                        }
                                    }

                                    obstacle_height = h;
                                    platformWithObstacle = true;
                                    leftEdge = LevelArray.ConvertValue_ArrayPointIntoPoint(j - 1);

                                    break;
                                }

                            }

                            // there is no obstacle in this platform
                            if (h == 25 && levelArray[i - h, j] != LevelArray.OBSTACLE && platformWithObstacle)
                            {

                                rightEdge = LevelArray.ConvertValue_ArrayPointIntoPoint(j - 1);

                                if (rightEdge >= leftEdge)
                                {
                                    lock (platformInfoList)
                                    {
                                        platformInfoList.Add(new PlatformInfo(0, height, leftEdge, rightEdge, new List<MoveInfo>(), obstacle_height * LevelArray.PIXEL_LENGTH));
                                    }
                                }

                                obstacle_height = NO_OBSTACLE;
                                platformWithObstacle = false;
                                leftEdge = LevelArray.ConvertValue_ArrayPointIntoPoint(j - 1);
                            }
                        }
                    }
                }
            });

            SetPlatformID();
        }

        public override void SetMoveInfoList(int[,] levelArray, int numCollectibles)
        {
            // con el array del nivel se revisan las plataformas para determinar el movmiento necesario en la plataforma
            // son caída inmediata, si es necesario usar morph (cambiar dimensiones del rectangulo)
            // o si bien es una escalera o caída
            SetMoveInfoList_StraightFall(levelArray, numCollectibles);
            SetMoveInfoList_Morph(levelArray, numCollectibles);
            SetMoveInfoList_StairOrGap(levelArray, numCollectibles);

            foreach (PlatformInfo i in platformInfoList)
            {
                int from = i.leftEdge + (i.leftEdge - GameInfo.LEVEL_ORIGINAL) % (LevelArray.PIXEL_LENGTH * 2);
                int to = i.rightEdge - (i.rightEdge - GameInfo.LEVEL_ORIGINAL) % (LevelArray.PIXEL_LENGTH * 2);

                Parallel.For(0, (to - from) / (LevelArray.PIXEL_LENGTH * 2) + 1, j =>
                {
                    for (int h = GameInfo.HORIZONTAL_RECTANGLE_HEIGHT; h <= GameInfo.VERTICAL_RECTANGLE_HEIGHT; h += 20)
                    {
                        LevelArray.Point movePoint = new LevelArray.Point(from + j * LevelArray.PIXEL_LENGTH * 2, i.height - (h / 2));
                        SetMoveInfoList_NoAction(levelArray, i, movePoint, numCollectibles, h);
                    }
                });

                Parallel.For(0, (GameInfo.MAX_VELOCITY_X / VELOCITYX_STEP), k =>
                {
                    int velocityX = VELOCITYX_STEP * k;

                    // left fall
                    LevelArray.Point movePoint = new LevelArray.Point(i.leftEdge - LevelArray.PIXEL_LENGTH, i.height - GameInfo.SQUARE_RADIUS);
                    SetMoveInfoList_Fall(levelArray, i, movePoint, velocityX, false, numCollectibles);

                    // right fall
                    movePoint = new LevelArray.Point(i.rightEdge + LevelArray.PIXEL_LENGTH, i.height - GameInfo.SQUARE_RADIUS);
                    SetMoveInfoList_Fall(levelArray, i, movePoint, velocityX, true, numCollectibles);
                });
            }
        }

        private void SetMoveInfoList_NoAction(int[,] levelArray, Platform.PlatformInfo fromPlatform, LevelArray.Point movePoint, int numCollectibles, int h)
        {
            bool[] collectible_onPath = new bool[numCollectibles];

            List<LevelArray.ArrayPoint> rectanglePixels = GetRectanglePixels(movePoint, h);

            if (IsObstacle_onPixels(levelArray, rectanglePixels))
            {
                return;
            }

            collectible_onPath = GetCollectibles_onPixels(levelArray, rectanglePixels, collectible_onPath.Length);

            AddMoveInfoToList(fromPlatform, new Platform.MoveInfo(fromPlatform, movePoint, movePoint, 0, true, movementType.NO_ACTION, collectible_onPath, 0, false, h));

        }

        private void SetMoveInfoList_StairOrGap(int[,] levelArray, int numCollectibles)
        {
            foreach (PlatformInfo fromPlatform in platformInfoList)
            {
                foreach (PlatformInfo toPlatform in platformInfoList)
                {
                    if (fromPlatform.Equals(toPlatform))
                    {
                        continue;
                    }

                    bool rightMove = false;

                    if (IsStairOrGap(fromPlatform, toPlatform, ref rightMove))
                    {
                        bool obstacleFlag = false;
                        bool[] collectible_onPath = new bool[numCollectibles];

                        int from = rightMove ? fromPlatform.rightEdge : toPlatform.rightEdge;
                        int to = rightMove ? toPlatform.leftEdge : fromPlatform.leftEdge;

                        for (int k = from; k <= to; k += LevelArray.PIXEL_LENGTH)
                        {
                            List<LevelArray.ArrayPoint> rectanglePixels = GetRectanglePixels(new LevelArray.Point(k, toPlatform.height - 50), GameInfo.SQUARE_HEIGHT);

                            if (IsObstacle_onPixels(levelArray, rectanglePixels))
                            {
                                obstacleFlag = true;
                                break;
                            }

                            /*
                             * POSSIVEL BUG PORQUE ESCREVE EM CIMA DO COLLECTIBLE_ONPATH
                             * 
                             * */

                            collectible_onPath = GetCollectibles_onPixels(levelArray, rectanglePixels, numCollectibles);
                        }

                        if (!obstacleFlag)
                        {
                            LevelArray.Point movePoint = rightMove ? new LevelArray.Point(fromPlatform.rightEdge, fromPlatform.height) : new LevelArray.Point(fromPlatform.leftEdge, fromPlatform.height);
                            LevelArray.Point landPoint = rightMove ? new LevelArray.Point(toPlatform.leftEdge, toPlatform.height) : new LevelArray.Point(toPlatform.rightEdge, toPlatform.height);

                            AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform, movePoint, landPoint, 0, rightMove, movementType.STAIR_GAP, collectible_onPath, (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false));
                        }
                    }

                    else if (toPlatform.height >= fromPlatform.height - 90 && toPlatform.height <= fromPlatform.height)
                    {

                        bool[] collectible_onPath = new bool[numCollectibles];

                        if (fromPlatform.rightEdge >= toPlatform.leftEdge - 33 && fromPlatform.rightEdge <= toPlatform.leftEdge)
                        {
                            rightMove = true;

                            LevelArray.Point movePoint = rightMove ? new LevelArray.Point(fromPlatform.rightEdge, fromPlatform.height) : new LevelArray.Point(fromPlatform.leftEdge, fromPlatform.height);
                            LevelArray.Point landPoint = rightMove ? new LevelArray.Point(toPlatform.leftEdge, toPlatform.height) : new LevelArray.Point(toPlatform.rightEdge, toPlatform.height);

                            AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform, movePoint, landPoint, 0, rightMove, movementType.STAIR_GAP, collectible_onPath, (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false, 200));

                        }
                        else if (fromPlatform.leftEdge <= toPlatform.rightEdge + 16 && fromPlatform.leftEdge >= toPlatform.rightEdge)
                        {
                            rightMove = false;

                            LevelArray.Point movePoint = rightMove ? new LevelArray.Point(fromPlatform.rightEdge, fromPlatform.height) : new LevelArray.Point(fromPlatform.leftEdge, fromPlatform.height);
                            LevelArray.Point landPoint = rightMove ? new LevelArray.Point(toPlatform.leftEdge, toPlatform.height) : new LevelArray.Point(toPlatform.rightEdge, toPlatform.height);

                            AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform, movePoint, landPoint, 0, rightMove, movementType.STAIR_GAP, collectible_onPath, (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false, 200));
                        }

                    }


                }
            }
        }

        private void SetMoveInfoList_Morph(int[,] levelArray, int numCollectibles)
        {
            foreach (PlatformInfo fromPlatform in platformInfoList)
            {
                foreach (PlatformInfo toPlatform in platformInfoList)
                {
                    if (fromPlatform.Equals(toPlatform) ||
                        fromPlatform.height != toPlatform.height)
                    {
                        continue;
                    }

                    bool rightMove;

                    if (fromPlatform.rightEdge == toPlatform.leftEdge)
                    {
                        rightMove = true;
                    }
                    else if (fromPlatform.leftEdge == toPlatform.rightEdge)
                    {
                        rightMove = false;
                    }
                    else
                    {
                        continue;
                    }

                    int from = rightMove ? fromPlatform.rightEdge : toPlatform.rightEdge;
                    int to = rightMove ? toPlatform.leftEdge : fromPlatform.leftEdge;
                    bool[] collectible_onPath = new bool[numCollectibles];

                    if (toPlatform.obstacleHeight > 0 && toPlatform.obstacleHeight < 100)
                    {

                        LevelArray.Point movePoint = rightMove ? new LevelArray.Point(fromPlatform.rightEdge - 100, fromPlatform.height) : new LevelArray.Point(fromPlatform.leftEdge + 100, fromPlatform.height);
                        //LevelArray.Point movePoint = rightMove ? new LevelArray.Point(fromPlatform.rightEdge - 50, fromPlatform.height) : new LevelArray.Point(fromPlatform.leftEdge + 50, fromPlatform.height);
                        //LevelArray.Point landPoint = rightMove ? new LevelArray.Point(toPlatform.leftEdge, toPlatform.height) : new LevelArray.Point(toPlatform.rightEdge, toPlatform.height);
                        LevelArray.Point landPoint = rightMove ? new LevelArray.Point(toPlatform.rightEdge + 100, toPlatform.height) : new LevelArray.Point(toPlatform.leftEdge - 100, toPlatform.height);

                        for (int k = from; k <= to; k += LevelArray.PIXEL_LENGTH)
                        {
                            List<LevelArray.ArrayPoint> rectanglePixels = GetRectanglePixels(new LevelArray.Point(k, toPlatform.height - (toPlatform.obstacleHeight / 2)), toPlatform.obstacleHeight);

                            collectible_onPath = GetCollectibles_onPixels(levelArray, rectanglePixels, numCollectibles);

                            AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform, movePoint, landPoint, 0, rightMove, movementType.MORPH_DOWN, collectible_onPath, (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false, toPlatform.obstacleHeight));
                        }
                    }
                    else
                    {
                        LevelArray.Point movePoint = rightMove ? new LevelArray.Point(fromPlatform.rightEdge, fromPlatform.height) : new LevelArray.Point(fromPlatform.leftEdge, fromPlatform.height);
                        LevelArray.Point landPoint = rightMove ? new LevelArray.Point(toPlatform.leftEdge, toPlatform.height) : new LevelArray.Point(toPlatform.rightEdge, toPlatform.height);

                        AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform, movePoint, landPoint, 0, rightMove, movementType.STAIR_GAP, new bool[numCollectibles], (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false));
                    }
                }
            }
        }

        private void SetMoveInfoList_StraightFall(int[,] levelArray, int numCollectibles)
        {
            foreach (PlatformInfo fromPlatform in platformInfoList)
            {
                foreach (PlatformInfo toPlatform in platformInfoList)
                {
                    // si es la misma plataforma
                    if (fromPlatform.Equals(toPlatform) ||
                        fromPlatform.height < toPlatform.height - 2 * LevelArray.PIXEL_LENGTH ||
                        fromPlatform.height > toPlatform.height + 2 * LevelArray.PIXEL_LENGTH)
                    {
                        continue;
                    }

                    // COMING FROM THE LEFT

                    if (toPlatform.leftEdge - fromPlatform.rightEdge < 150 && toPlatform.leftEdge - fromPlatform.rightEdge > 50)
                    {
                        int movePoint_X = fromPlatform.rightEdge + ((toPlatform.leftEdge - fromPlatform.rightEdge) / 2) + LevelArray.PIXEL_LENGTH;
                        int movePoint_Y = fromPlatform.height - (GameInfo.HORIZONTAL_RECTANGLE_HEIGHT / 2);
                        LevelArray.Point movePoint = new LevelArray.Point(movePoint_X, movePoint_Y);

                        SetMoveInfoList_Fall_Special(levelArray, fromPlatform, movePoint, 0, true, numCollectibles);

                        movePoint = new LevelArray.Point(fromPlatform.rightEdge, fromPlatform.height);
                        LevelArray.Point landPoint = new LevelArray.Point(toPlatform.leftEdge, toPlatform.height);

                        bool[] collectible_onPath = new bool[numCollectibles];

                        // Tratar los collecionables y los obstaculos
                        AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform, movePoint, landPoint, 0, true, movementType.STAIR_GAP, collectible_onPath, (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false, 50));


                    }

                    // COMING FROM THE RIGHT

                    if (fromPlatform.leftEdge - toPlatform.rightEdge < 150 && fromPlatform.leftEdge - toPlatform.rightEdge > 50)
                    {
                        int movePoint_X = fromPlatform.leftEdge - ((fromPlatform.leftEdge - toPlatform.rightEdge) / 2) - LevelArray.PIXEL_LENGTH;
                        int movePoint_Y = fromPlatform.height - (GameInfo.HORIZONTAL_RECTANGLE_HEIGHT / 2);
                        LevelArray.Point movePoint = new LevelArray.Point(movePoint_X, movePoint_Y);

                        SetMoveInfoList_Fall_Special(levelArray, fromPlatform, movePoint, 0, false, numCollectibles);

                        movePoint = new LevelArray.Point(fromPlatform.leftEdge, fromPlatform.height);
                        LevelArray.Point landPoint = new LevelArray.Point(toPlatform.rightEdge, toPlatform.height);

                        bool[] collectible_onPath = new bool[numCollectibles];

                        /*
                         * VAI SER PRECISO TRATAR DOS COLLECTIBLES E OBSTACULOS
                         */

                        AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform, movePoint, landPoint, 0, false, movementType.STAIR_GAP, collectible_onPath, (fromPlatform.height - toPlatform.height) + Math.Abs(movePoint.x - landPoint.x), false, 50));
                    }

                }
            }
        }

        private void SetMoveInfoList_Fall_Special(int[,] levelArray, PlatformInfo fromPlatform, LevelArray.Point movePoint, int velocityX, bool rightMove, int numCollectibles)
        {
            if (!IsEnoughLengthToAccelerate(fromPlatform, movePoint, velocityX, rightMove))
            {
                return;
            }

            bool[] collectible_onPath = new bool[numCollectibles];
            float pathLength = 0;

            LevelArray.Point collidePoint = movePoint;
            LevelArray.Point prevCollidePoint;

            collideType collideType = collideType.OTHER;
            float collideVelocityX = rightMove ? velocityX : -velocityX;
            float collideVelocityY = GameInfo.FALL_VELOCITY;
            bool collideCeiling = false;

            do
            {
                prevCollidePoint = collidePoint;

                // el tipo de colision se cambia en la funcion GetPathInfo
                GetPathInfo(levelArray, collidePoint, collideVelocityX, collideVelocityY,
                    ref collidePoint, ref collideType, ref collideVelocityX, ref collideVelocityY, ref collectible_onPath, ref pathLength, 25);

                if (collideType == collideType.CEILING)
                {
                    collideCeiling = true;
                }

                if (prevCollidePoint.Equals(collidePoint))
                {
                    break;
                }
            }
            while (!(collideType == collideType.FLOOR));

            if (collideType == collideType.FLOOR)
            {
                PlatformInfo? toPlatform = GetPlatform_onRectangle(collidePoint, 100);

                if (toPlatform.HasValue)
                {
                    movePoint.x = rightMove ? movePoint.x - LevelArray.PIXEL_LENGTH : movePoint.x + LevelArray.PIXEL_LENGTH;

                    AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform.Value, movePoint, collidePoint, velocityX, rightMove, movementType.FALL, collectible_onPath, (int)pathLength, collideCeiling));
                }
            }
        }

        private void SetMoveInfoList_Fall(int[,] levelArray, PlatformInfo fromPlatform, LevelArray.Point movePoint, int velocityX, bool rightMove, int numCollectibles)
        {
            if (!IsEnoughLengthToAccelerate(fromPlatform, movePoint, velocityX, rightMove))
            {
                return;
            }

            bool[] collectible_onPath = new bool[numCollectibles];
            float pathLength = 0;

            LevelArray.Point collidePoint = movePoint;
            LevelArray.Point prevCollidePoint;

            collideType collideType = collideType.OTHER;
            float collideVelocityX = rightMove ? velocityX : -velocityX;
            float collideVelocityY = GameInfo.FALL_VELOCITY;
            bool collideCeiling = false;

            do
            {
                prevCollidePoint = collidePoint;

                GetPathInfo(levelArray, collidePoint, collideVelocityX, collideVelocityY,
                    ref collidePoint, ref collideType, ref collideVelocityX, ref collideVelocityY, ref collectible_onPath, ref pathLength, GameInfo.SQUARE_RADIUS);

                if (collideType == collideType.CEILING)
                {
                    collideCeiling = true;
                }

                if (prevCollidePoint.Equals(collidePoint))
                {
                    break;
                }
            }
            while (!(collideType == collideType.FLOOR));

            if (collideType == collideType.FLOOR)
            {

                PlatformInfo? toPlatform = GetPlatform_onRectangle(collidePoint, 100);

                if (toPlatform.HasValue)
                {
                    movePoint.x = rightMove ? movePoint.x - LevelArray.PIXEL_LENGTH : movePoint.x + LevelArray.PIXEL_LENGTH;

                    AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform.Value, movePoint, collidePoint, velocityX, rightMove, movementType.FALL, collectible_onPath, (int)pathLength, collideCeiling));
                }
            }
        }

        public Platform.PlatformInfo? GetPlatform_onRectangle(LevelArray.Point rectangleCenter, float height)
        {
            foreach (Platform.PlatformInfo i in platformInfoList)
            {
                if (i.leftEdge <= rectangleCenter.x && rectangleCenter.x <= i.rightEdge && 0 <= (i.height - rectangleCenter.y) && (i.height - rectangleCenter.y) <= height)
                {
                    return i;
                }
            }

            return null;
        }

        private List<LevelArray.ArrayPoint> GetRectanglePixels(LevelArray.Point rectangleCenter, int height)
        {
            LevelArray.ArrayPoint rectangleCenterArray = LevelArray.ConvertPointIntoArrayPoint(rectangleCenter, false, false);

            int rectangleHighestY = LevelArray.ConvertValue_PointIntoArrayPoint(rectangleCenter.y - (height / 2), false);
            int rectangleLowestY = LevelArray.ConvertValue_PointIntoArrayPoint(rectangleCenter.y + (height / 2), true);

            float rectangleWidth = GameInfo.RECTANGLE_AREA / height;
            int rectangleLeftX = LevelArray.ConvertValue_PointIntoArrayPoint((int)(rectangleCenter.x - (rectangleWidth / 2)), false);
            int rectangleRightX = LevelArray.ConvertValue_PointIntoArrayPoint((int)(rectangleCenter.x + (rectangleWidth / 2)), true);

            List<LevelArray.ArrayPoint> rectanglePixels = new List<LevelArray.ArrayPoint>();

            for (int i = rectangleHighestY; i <= rectangleLowestY; i++)
            {
                for (int j = rectangleLeftX; j <= rectangleRightX; j++)
                {
                    rectanglePixels.Add(new LevelArray.ArrayPoint(j, i));
                }
            }

            return rectanglePixels;
        }
    }
}
