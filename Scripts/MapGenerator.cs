using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapGenerator : MonoBehaviour
{
    public int width;
    public int height;
    public string seed; //Начальное состояние для System.Random
    public Room mainRoom;

    [Range(0, 100)]
    public int randomFillPercent;
    public int[,] map;
    public int [,] borderMap;
    public List<Room> roomss;

    void Start()
    {
        //GenerateMap();
    }

    private void Awake()
    {
    }

    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    GenerateMap();
        //}

    }

    //Вызов процедур генерации карты, сглаживания, удаления маленьких участков поверхностей и процедуры генерации меша
    public void GenerateMap()
    {
        map = new int[width, height];
        RandomFillMap();

        for (int i = 0; i < 5; i++)
        {
            SmoothMap();
        }

        ProcessMap();

        //Создание окаймляющей рамки вокург карты
        int borderSize = 5;
        borderMap = new int [width + borderSize * 2, height + borderSize * 2];
        for (int x = 0; x < borderMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                {
                    borderMap[x, y] = map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderMap[x, y] = 1;
                }
            }
        }

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        //Вызов процедур генерации меша
        //borderMap - массив, содержащий карту
        //1 - размер маршевого квадрата
        meshGen.GenerateMesh(borderMap, .8f);
        //Вызов процедуры рассылки карты for_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pasha
        //Вызов процедуры респауна for_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pashafor_Pasha
    }

    //Процедура удаления маленьких участков поверхностей
    //Закрашивает небольшие пустоты, удаляет маленькие объекты поверхности
    void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);
        int wallThresholdSize = 50; //Площадь объекта, необходимая для его выживания

        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach(Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions(0);
        int roomThresholdSize = 50; //Площадь объекта, необходимая для его выживания
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = 1;
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));  //Добавление выжившего объекта(комнаты) в список комнат
            }
        }
        //Выбор наибольшей из комнат в качестве главной
        survivingRooms.Sort();
        survivingRooms[0].isMainRoom = true;
        survivingRooms[0].isAccessibleFromMainRoom = true;

        //Сохранение главной комнаты для дальнейшего респауна игроков в ней
        mainRoom = new Room();
        mainRoom = survivingRooms[0];

        roomss = new List<Room>();
        roomss = survivingRooms;

        //Вызов процедуры соединения изолированных комнат
        //survivingRoom - список имеющихся комнат
        ConnectClosesRooms(survivingRooms);
    }

    //Рекурсивная процедура нахождения двух комнат, подлежащих соединению
    //allRooms - список всех имеющихся комнат
    //forceAccessibilityFromMainRoom - флаг необходимсоти соединения комнат с главной
    //Сначала соединяются все комнаты, еще не имеющие соединений с ближайшей комнатой. Но это не гарантирует 
    //отстутствие изолированных комнат. Затем устанавливаем флаг forceAccessibilityFromMainRoom и принудительно 
    //соединяем комнаты с ближайшей комнатой, которая имеет доступ к главной(или прямо к главной).
    void ConnectClosesRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();    //список комнат без доступа к главной
        List<Room> roomListB = new List<Room>();    //список комнат, имеющих доступ к главной

        //если forceAccessibilityFromMainRoom = 1, заполняем списки путем сортировки комнат между списками по признаку наличия соединения с главной
        if (forceAccessibilityFromMainRoom)
        {
            foreach (Room room in allRooms)
            {
                if (room.isAccessibleFromMainRoom)
                {
                    roomListB.Add(room);
                }
                else
                {
                    roomListA.Add(room);
                }
            }
        }
        //если forceAccessibilityFromMainRoom = 0, заполняем списки всеми комнаты, и будем стремится соеднинить их все между собой
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;   //флаг нахождения двух комнат для соединения

        //если forceAccessibilityFromMainRoom = 0, перебором находим для комнаты без соединений ближайшую комнату 
        //если forceAccessibilityFromMainRoom = 1, перебором находим для комнаты без доступа к главной ближайшую комнату с доступом к главной 
        foreach (Room roomA in roomListA)
        {
            if (!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                {
                    continue;
                }
            }
            
            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                {
                    continue;
                }
                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Math.Pow(tileA.tileX - tileB.tileX, 2) + Math.Pow(tileA.tileY - tileB.tileY, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }
            if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }
        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosesRooms(allRooms, true);
        }

        if (!forceAccessibilityFromMainRoom)
        {
            ConnectClosesRooms(allRooms, true);
        }
    }

    //Процедура соединения 2 комнат между собой
    //roomA - первая комната
    //roomB - вторая комната
    //tileA, tileB - ближайшие между собой плитки двух комнат
    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        List<Coord> line = GetLine(tileA, tileB);
        foreach(Coord c in line)
        {
            DrawCircle(c, 4);
        }
    }

    //Процедура, формирующая путь путем "вырезания" "окружности" заданного радиуса из поверхности
    //c - центр "окружности"
    //r - радиус "окружности"
    void DrawCircle(Coord c, int r)
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x*x + y*y <= r*r)
                {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if (IsInMapRange(drawX, drawY))
                    {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }

    //Процедура, формирующая путь между точками A и B как список координат прямой из точки А в Б
    //from - точка A
    //to - точка B
    //Используются приросты по осям X и Y, и инверсию этих приростов для просчета прямой в различных четвертях
    List<Coord> GetLine(Coord from, Coord to)
    {
        List<Coord> line = new List<Coord>();
        int x = from.tileX;
        int y = from.tileY;

        bool inverted = false;
        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Math.Abs(dx);
        int shortest = Math.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Math.Abs(dy);
            shortest = Math.Abs(dx);
            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }
        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x, y));

            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }
            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }
            }
            gradientAccumulation -= longest;
        }
        return line;
    }
        
    //Процедура перевода координат ячейки массива в координаты плитки на карте
    public Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-width*.8f / 2 + .8f*tile.tileX + .8f / 3, -height*.8f / 2 + .8f / 3 + .8f*tile.tileY, 0);
    }

    //Процедура, формирующая список изолированных регионов, заполненных одним типом плитки
    //tileType - тип плитки
    List<List<Coord>> GetRegions (int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);
                    
                    foreach (Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }
        return regions;
    }

    //Процедура, формирующая список плиток для одного региона, заполненного тем или иным типом плитки
    //startX, startY - стартовые координаты для формирования региона
    //Используется приницип водопада. В очередь помещается очередная плитка, затем она извлекается и просматриваются плитки по горизонтали 
    //и вертикали от текущей на +-1. Если они того же типа, что и стартовая плитка, то они заносятся в очередь. Итерации повторяются, пока очередь не опустеет.
    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            queue.Enqueue(new Coord(x, y));
                            mapFlags[x, y] = 1;
                        }
                    }
                }
            }
        }
        return tiles;
    }

    //Процедура, проверяющая находится ли плитка в пределах карты
    //x, y - координаты плитки
    public bool IsInMapRange (int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    //Процедура заполнения массива псевдослучайными элементами (0 или 1)
    void RandomFillMap()
    {
        //Создаем рандомайзер
        System.Random rnd = new System.Random(seed.GetHashCode());

        //Заполняем массив
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    //Учитываем насколько процентов необходимо заполнить карту плитками с поверхностью
                    map[x, y] = (rnd.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    //Процедура первоначального формирования карты из случайно заполненного массива
    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int mapWallTiles = GetSurroundingWallCount(x, y);
                if (mapWallTiles > 3)
                    map[x, y] = 1;
                else if (mapWallTiles <= 3)
                {
                    map[x, y] = 0;
                }
            }
        }
    }

    //Процедура подсчета количества плиток с поверхностью в области 3*3 вокруг передаваемого элемента
    //mapX, mapY - координаты элемента, около которого необходимом подсчитать кол-во плиток
    int GetSurroundingWallCount(int mapX, int mapY)
    {
        int wallCount = -1;
        for (int neighboutX = mapX - 1; neighboutX <= mapX + 1; neighboutX++)
        {
            for (int neighboutY = mapY - 1; neighboutY <= mapY + 1; neighboutY++)
            {
                if (IsInMapRange(neighboutX, neighboutY))
                {
                    wallCount += map[neighboutX, neighboutY];
                }
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    //Структура для хранения плиток
    public struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    //Класс для описания комнаты
    public class Room : IComparable<Room>
    {
        public List<Coord> tiles;   //список всех плиток комнаты
        public List<Coord> edgeTiles;  //список граничных комнаты
        public List<Room> connectedRooms;   //список присоединенных комнат
        public int roomSize;
        public bool isAccessibleFromMainRoom;   //флаг наличия соединения с главной комнатой
        public bool isMainRoom; //флаг главной комнаты

        public Room()
        {

        }

        public Room(List<Coord> roomTiles, int[,] map)
        {
            tiles = roomTiles;
            roomSize = roomTiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach (Coord tile in tiles)
            {
                for(int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for(int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if (x == tile.tileX || y == tile.tileY)
                        {
                            if (map[x, y] == 1)
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }

        //метод установления соединения текущей команты, а также всех комнат, соединенных с текущей, с главной комнатой
        public void SetAccessibleFromMainRoom()
        {
            if (!isAccessibleFromMainRoom)
            {
                isAccessibleFromMainRoom = true;
                foreach(Room connectedRoom in connectedRooms)
                {
                    connectedRoom.SetAccessibleFromMainRoom();
                }
            }
        }

        //метод установления соединения двух комнат
        //roomA, roomB - команты для соединения
        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.isAccessibleFromMainRoom)
            {
                roomB.SetAccessibleFromMainRoom();
            }
            else if (roomB.isAccessibleFromMainRoom)
            {
                roomA.SetAccessibleFromMainRoom();
            }

            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        //метод, определяющий соединены ли текущая комната с какой-либо
        //otherRoom - комната, с которой проверяется наличие соединения
        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }

        //метод для сравнения текущей комнаты с какой-либо по признаку размера
        //otherRoom - комната, с которой проводится сравнение
        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }
    }
}