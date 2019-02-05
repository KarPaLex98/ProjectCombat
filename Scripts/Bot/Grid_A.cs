using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grid_A : MonoBehaviour {

	public bool displayGridGizmos;
	public LayerMask unwalkableMask; //Маска для непроходимых объектов
	public Vector2 gridWorldSize; //Размер сетки
	public float nodeRadius;      //Радиус узла
	public TerrainType[] walkableRegions; //Проходимые регионы
	public int obstacleProximityPenalty = 10; //Штраф за расстояние до препятствия
	LayerMask walkableMask; //Проходимая маска
	Dictionary<int, int> walkableRegionDictionary = new Dictionary<int, int>();
    //Таблица
	Node[,] grid;

	float nodeDiameter; //Диаметр узла
	int gridSizeX, gridSizeY; //размеры сетки по X и Y

    //Для визуализации
	int penaltyMin = int.MaxValue;
	int penaltyMax = int.MinValue;

	public void Awake() {
		
	}

    public void CreateGridStart()
    {

        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        foreach (TerrainType region in walkableRegions)
        {
            walkableMask.value |= region.terrainMask.value;
            walkableRegionDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
        }

        CreateGrid();

    }

    public void test()
    {
    }

    //получение максимального размера кучи
	public int MaxSize {
		get {
			return gridSizeX * gridSizeY;
		}
	}

    //Создание сетки
	void CreateGrid() {
       
		grid = new Node[gridSizeX,gridSizeY];
        //Левый низ, позици - (1.0.0)*размер по x/2 - (0.0.1)*размер по Y/2
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.up * gridWorldSize.y/2;

        //Заполняем наш grid 
		for (int x = 0; x < gridSizeX; x ++) {
			for (int y = 0; y < gridSizeY; y ++) {

                //Находим точки снизу слева, движение по циклу
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius);
                //Проходимая ли нода
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius + .5f ,unwalkableMask));
				
                //Штраф движения
				int movementPenalty = 0;

                //Луч от высоты 50 до -1 
				Ray ray = new Ray(worldPoint + Vector3.forward * 50, Vector3.back);
                //Структура используется, чтобы получить информацию от raycast.
                RaycastHit hit;
                //Raycast, начало, направление, дистанция, слой, чтоб игнорировать коллайдеры
				if (Physics.Raycast(ray, out hit, 100, walkableMask)) {
					walkableRegionDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
				}

				if (!walkable) {
					movementPenalty += obstacleProximityPenalty;
				}

				grid[x,y] = new Node(walkable, worldPoint, x, y, movementPenalty);
			}
		}

        //Размытие карты
		BlurPenaltyMap(3);
	}

//Карта размытия штрафа
void BlurPenaltyMap(int blurSize) {
        //Размер ядра
		int kernelSize = blurSize * 2 + 1;
		int kernelExtents = (kernelSize - 1) / 2;

        //проход пенальти по горизонтали ниже по вертикали
		int[,] penaltiesHorizontalPass = new int[gridSizeX,gridSizeY];
		int[,] penaltiesVerticalPass = new int[gridSizeX,gridSizeY];

        //Бегаем по столбикам
		for (int y = 0; y < gridSizeY; y++) {
            
            //Прибавяем сумму всех трех квадратиков к 
            for (int x = -kernelExtents; x <= kernelExtents; x++) {
				int sampleX = Mathf.Clamp (x, 0, kernelExtents);
				penaltiesHorizontalPass [0, y] += grid [sampleX, y].movementPenalty;
			}

            //Проверям что не выходит слева и справа за сетку
			for (int x = 1; x < gridSizeX; x++) {
				int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
				int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX-1);

				penaltiesHorizontalPass [x, y] = penaltiesHorizontalPass [x - 1, y] - grid [removeIndex, y].movementPenalty + grid [addIndex, y].movementPenalty;
			}
		}
		
        //Тоже самое уже по строчкам
		for (int x = 0; x < gridSizeX; x++) {
			for (int y = -kernelExtents; y <= kernelExtents; y++) {
				int sampleY = Mathf.Clamp (y, 0, kernelExtents);
				penaltiesVerticalPass [x, 0] += penaltiesHorizontalPass [x, sampleY];
			}

			int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, 0] / (kernelSize * kernelSize));
			grid [x, 0].movementPenalty = blurredPenalty;

			for (int y = 1; y < gridSizeY; y++) {
				int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
				int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY-1);

				penaltiesVerticalPass [x, y] = penaltiesVerticalPass [x, y-1] - penaltiesHorizontalPass [x,removeIndex] + penaltiesHorizontalPass [x, addIndex];
                //Результирующе размытый штраф
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, y] / (kernelSize * kernelSize));
				grid [x, y].movementPenalty = blurredPenalty;

				if (blurredPenalty > penaltyMax) {
					penaltyMax = blurredPenalty;
				}
				if (blurredPenalty < penaltyMin) {
					penaltyMin = blurredPenalty;
				}
			}
		}

	}

    //Получаем лист соседей
	public List<Node> GetNeighbours(Node node) {
		List<Node> neighbours = new List<Node>();

            
		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
                //Это текущая нода, её проверять не нужно
                if (x == 0 && y == 0)
                {
                    continue;
                }

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

                //Добовляем соседей, если они не заграницами
				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
					neighbours.Add(grid[checkX,checkY]);
				}
			}
		}
        // возвращаем всех соседей для текущей ноды
		return neighbours;
	}
	

	public Node NodeFromWorldPoint(Vector3 worldPosition) {
		float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
		float percentY = (worldPosition.y + gridWorldSize.y/2) / gridWorldSize.y;
		percentX = Mathf.Clamp01(percentX);
		percentY = Mathf.Clamp01(percentY);

        //Положение ноды на сетке
		int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY-1) * percentY);

		return grid[x,y];
	}
	
    //Рисуем сетку из кубов
	void OnDrawGizmos() {
        //Рисует каркасный куб с центром в точке center и размером size.
        Gizmos.DrawWireCube(transform.position,new Vector3(gridWorldSize.x,gridWorldSize.y,1));
		if (grid != null && displayGridGizmos) {
			foreach (Node n in grid) {
				Gizmos.color = Color.Lerp (Color.white, Color.black, Mathf.InverseLerp (penaltyMin, penaltyMax, n.movementPenalty));
				Gizmos.color = (n.walkable)?Gizmos.color:Color.red;
				Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter-.1f));
			}
		}
	}

    //Тип местности и её штраф
	[System.Serializable]
	public class TerrainType {
		public LayerMask terrainMask;
		public int terrainPenalty;
	}
}