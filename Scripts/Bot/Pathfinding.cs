using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Pathfinding : MonoBehaviour {

    Grid_A grid;

    //вызывается когда экземпляр скрипта будет загружен.
    void Awake()
    {
        grid = GetComponent<Grid_A>();
    }

    //Находит путь, request - стартовая позиция и конечная
    public void FindPath(PathRequest request, Action<PathResult> callback)
    {
        //Массив путевых точек
        Vector3[] waypoints = new Vector3[0];
        //Успешный ли путь
        bool pathSuccess = false;

        //Стартовая нода
        Node startNode = grid.NodeFromWorldPoint(request.pathStart);
        //Конечная нода
        Node targetNode = grid.NodeFromWorldPoint(request.pathEnd);
        
        if (startNode.walkable && targetNode.walkable) {
            //Точки которые нужно оценить
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
            //Точки которые уже оценены
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);
			
            //Если есть еще не просмотренные точки
			while (openSet.Count > 0) {
                //текущая нода = 
                Node currentNode = openSet.RemoveFirst();
				closedSet.Add(currentNode);
				
                //Если достигли цели
				if (currentNode == targetNode) {
                    //Путь успешный
					pathSuccess = true;
					break;
				}
				
                //Бегаем по всем соседям для текущей ноды
				foreach (Node neighbour in grid.GetNeighbours(currentNode)) {
                    //Пропускаем соседа, если он преграда или лежит в просмотренной куче
                    if (!neighbour.walkable || closedSet.Contains(neighbour)) {
						continue;
					}
					
                    //Новая стоймость до соседа, расстояние от тек ноды + дистанция от тек ноды до соседа 
					int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.movementPenalty;

                    //Если стоймость до соседа меньше чем расстояние от соседа или соседа нет в просм точках
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {

                        neighbour.gCost = newMovementCostToNeighbour;// стоймость до соседа = новой стоймости
						neighbour.hCost = GetDistance(neighbour, targetNode); //Находим растояние от соседа до цели
						neighbour.parent = currentNode; //текущая нода становится родителем для соседа
						
                        //Если нет добавляем, иначе обновляем данные
						if (!openSet.Contains(neighbour)) {
							openSet.Add(neighbour);
						} else {
							openSet.UpdateItem(neighbour);
						}
						
					}
				}
			}
		}
        //Если путь успешный
		if (pathSuccess) {
            //Записать в массив точек путь
			waypoints = RetracePath(startNode,targetNode);
            pathSuccess = waypoints.Length > 0;
		}
        callback(new PathResult(waypoints, pathSuccess, request.callback));
    }
	
    //Вернуть путь от стартовой ноды до конечной (цели)
	Vector3[] RetracePath(Node startNode, Node endNode) {
        
        List<Node> path = new List<Node>();
        
        Node currentNode = endNode;

        //От конца до начала строим
        while (currentNode != startNode) {
			path.Add(currentNode);
			currentNode = currentNode.parent;
		}

		Vector3[] waypoints = SimplifyPath(path);
        //Реверсируем список, чтоб вернуть от начально до конечной
        Array.Reverse(waypoints);
		return waypoints;
		
	}
	
    //Упрощенный путь, точки там где путь меняет свое направление
	Vector3[] SimplifyPath(List<Node> path) {
		List<Vector3> waypoints = new List<Vector3>();
        //направление двух последних узлов
        Vector2 directionOld = Vector2.zero;
		
		for (int i = 1; i < path.Count; i++) {
			Vector2 directionNew = new Vector2(path[i-1].gridX - path[i].gridX, path[i-1].gridY - path[i].gridY);
			if (directionNew != directionOld) {
				waypoints.Add(path[i].worldPosition);
			}
			directionOld = directionNew;
		}
		return waypoints.ToArray();
	}

    //Вернуть дистанцию (эврестическая функция) 
    //Расстояние Чебышева применяется, когда к четырем направлениям добавляются диагонали:
    int GetDistance(Node nodeA, Node nodeB) {

        //Модуль X первой ноды - Х второй ноды
		int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        //Модуль Y первой ноды - Y второй ноды
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
		
		if (dstX > dstY)
			return 14*dstY + 10* (dstX-dstY);
		return 14*dstX + 10 * (dstY-dstX);
	}
	
	
}