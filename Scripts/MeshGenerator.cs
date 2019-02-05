using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public SquareGrid squareGrid;
    public MeshFilter walls;
    public MeshFilter cave;
    public bool is2D;

    List<Vector3> vertices; //Список вершин меша
    List<int> triangles;    //Список треугольников из этих вершин

    //Словарь, связывающий вершины и треугольники. Ключ - индекс вершины. Возврат - все треугольники, в составе которых содержится вершинакоторым она принадлежит
    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();

    //Список всех найденных контуров, где контур - это набор вершин
    List<List<int>> outlines = new List<List<int>>();

    //Список уже проверенных вершин, которые не являются карйними в меше. Такие вершины не подлежат дальнейшей проверке
    HashSet<int> checkedVertices = new HashSet<int>();

    //Струкутра для хранения вершин треугольника
    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        //Метод, возврашающий вершину с индексом i
        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }

        //Метод, определяющий содержится ли вершина в данном треугольнике
        public bool Contains (int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    //Процедура, преобразующая массив в координаты в виде маршевых квадратов. Они являются основой основой для меша. Также процедура осуществляет непосредственно построение меша (сетки)
    public void GenerateMesh(int[,] map, float squareSize)
    {
        outlines.Clear();
        checkedVertices.Clear();
        triangleDictionary.Clear();

        squareGrid = new SquareGrid(map, squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        //Разбиение каждого элемента массива на маршевый квадрат и его триангуляция
        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        //Создание меша и назначение его компоненту MeshFilter, а также обновление массивов вершин и треугольников
        Mesh mesh = new Mesh();
        cave.mesh = mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        //Текстурирование меша
        int tileAmount = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].x) * tileAmount;
            float percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].y) * tileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
        mesh.uv = uvs;

        //Определение режима генерации карту. Возможно использование карты как трехмерного лабиринта при повороте объектов на 90 градусов
        if (is2D)
        {
            Generate2DColliders();
            CreateWallMesh();
        }
        else
        {
            CreateWallMesh();
        }
    }

    //Процедура создания меша для стен(3D) или пола(2D), по которому передвигается персонаж
    void CreateWallMesh()
    {
        CalculateMeshOutlines();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = 5;

        //Проходим все контуры в списке контуров
        foreach (List<int> outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]); //Ближняя левая врешина
                wallVertices.Add(vertices[outline[i + 1]]); //Ближняя правая вершина
                //!Поменял up на back
                wallVertices.Add(vertices[outline[i]] - Vector3.back * wallHeight); //Дальняя левая вершина
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.back * wallHeight); //Дальняя правая вершина

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }
        //Создание меша и назначение его компоненту MeshFilter для контура (пола), а также обновление массивов вершин и треугольников
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;

        //Добавление коллайдеров для  для стен(3D) или пола(2D)
        if (walls.gameObject.GetComponent<MeshCollider>() == null)
        {
            MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider>();
            wallCollider.sharedMesh = wallMesh;
        }
        else
        {
            MeshCollider wallCollider = walls.gameObject.GetComponent<MeshCollider>();
            wallCollider.sharedMesh = wallMesh;
        } 
        
    }

    //Процедура, создающая коллайдеры на меше. Проходит по контурам карты и формирует EdgeCollider2D
    void Generate2DColliders()
    {

        EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();
        for (int i = 0; i < currentColliders.Length; i++)
        {
            Destroy(currentColliders[i]);
        }

        CalculateMeshOutlines();

        foreach (List<int> outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];

            for (int i = 0; i < outline.Count; i++)
            {
                edgePoints[i] = new Vector2(vertices[outline[i]].x, vertices[outline[i]].y);
            }
            edgeCollider.points = edgePoints;
        }

    }

    //Процедура для разбития маршевого квадрата на треугольники (всего 16 комбинаций)
    //square - маршевый квадрат
    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            //Случаи с 1 активной точкой
            case 1:
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;

            //Случаи с 2 активными точками
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5:
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            //Случаи с 3 активными точками
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            //Случаи с 4 активными точками
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                //Тут все точки подлежат отрисовке и не могут быть граничными, поэтому их не надо учитывать
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    //Процедура для формирование списка вершин и треугольников
    //Процедура AssignVertices добавляет вершины в список и сохраняет их индекс. Добавление производится только если вершины еще нет в списке
    //Процедура CreateTriangle последовательно записывает индексы 3 вершин треугольника из списка вершин в список треугольников. Так 3 вершины связываются друг с другом
    //Если маршевый квдарат разбивается на несколько треугольников, то индексы следующих треугольников записываются 
    //points - массив точек
    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);
    }

    //Процедура для составления списка вершин
    //points - массив точек
    void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    //Процедура для составления списка треугольников по номеру вершин
    //a,b,c - вершины треугольника
    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    //Процедура, добавляющая треугольник в словарь, 
    //при этом если ключ (индекс) уже имеется в словаре, 
    //то треугольник просто добавляется в список для этого ключа
    //vertexIndex - индекс вершины
    //triangle - добавляемый треугольник
    void AddTriangleToDictionary(int vertexIndex, Triangle triangle)
    {
        if (triangleDictionary.ContainsKey(vertexIndex))
        {
            triangleDictionary[vertexIndex].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndex, triangleList);
        }
    }

    //Процедура, которая проходит каждую вершину меша и создает контур пока не дойдет до исходной точки контура.
    //Если найдена крайняя точка, то процедура FollowOutline получает исходную точку контура и сам конутур, который сейчас строится
    //Затем происходит рекурсивный поиск следующих точек контура, пока он не замкнется
    void CalculateMeshOutlines()
    {
        for (int vertexIndex= 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    //Процедура, которая записывает следущую за исходной точкой точку контура в список конутра и далее находит следующие
    //точки текущего контура, пока не дойдет до уже проверенных в текущем контуре
    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    //Процедура, которая перебирает вершины всех треугольников, содержащие данную вершину, на предмет образования
    //с данной вершиной ребра, которое является крайним в сгенерированном меше. Позволяет определить, является ли данная вершина крайней
    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];
        for (int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertex[i];
            for (int j = 0; j <  3; j++)
            {
                int vertexB = triangle[j];

                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutLineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }
        return -1;
    }

    //Процедура, которая определяет, является ли ребро, заданное 2 вершинами, крайним в сгенерированном меше
    //Если ребро принадлежит лишь одному треугольнику, то ребро крайнее
    bool IsOutLineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1)
                    break;
            }
        }
        return sharedTriangleCount == 1;
    }

    //Класс для объектов, которые представляют собой таблицу из маршевых квадратов
    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidht = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;
       
            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    //!Поменял местами оси Y и Z
                    Vector3 pos = new Vector3(-mapWidht / 2 + x * squareSize + squareSize / 3, -mapHeight / 2 + y * squareSize + squareSize / 3, 0);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    //!Зачем-то повернули узлы в углах квдарата?
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x, y], controlNodes[x + 1, y]);
                }
            }
        }
    }

    //Класс для объектов, которые представляют собой маршевый квдарт из всех нижеперечисленных узлов
    public class Square
    {
        public ControlNode topLeft, topRight, bottomLeft, bottomRight;
        public Node centerTop, centerLeft, centerBottom, centerRight;

        public int configuration;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomLeft, ControlNode _bottomRight)
        {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomLeft = _bottomLeft;
            bottomRight = _bottomRight;

            centerTop = topLeft.rightNode;
            centerLeft = bottomLeft.aboveNode;
            centerBottom = bottomLeft.rightNode;
            centerRight = bottomRight.aboveNode;

            //Подсчет кол-ва вершин, подлежащих отрисовке (т.е. заполненных 1)
            if (topLeft.active)
                configuration += 8;
            if (topRight.active)
                configuration += 4;
            if (bottomRight.active)
                configuration += 2;
            if (bottomLeft.active)
                configuration += 1;
        }
    }

    //Класс для объектов, которые представляют собой узлы, которые лежат посередине ребер маршевого квадрата
    public class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }

    //Класс для объектов, которые представляют собой вершины маршевого квадрата
    public class ControlNode : Node
    {
        public bool active;
        public Node aboveNode, rightNode;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos)
        {
            active = _active;
            //!Поменял above на up
            aboveNode = new Node(position + Vector3.up * squareSize / 3f);
            rightNode = new Node(position + Vector3.right * squareSize / 3f);
        }
    }
}