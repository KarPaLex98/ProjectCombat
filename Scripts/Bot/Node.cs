using UnityEngine;
using System.Collections;

public class Node : IHeapItem<Node> {
	
	public bool walkable;
	public Vector2 worldPosition; //Позиция
	public int gridX; //X, квадрата
	public int gridY; //Y
    public int movementPenalty; //Штраф движения

	public int gCost; //Расстояние от начального узла
	public int hCost; //Расстаяние от конечного узла (эврестический)
	public Node parent; 
	int heapIndex;
	
	public Node(bool _walkable, Vector2 _worldPos, int _gridX, int _gridY, int _penalty) {
		walkable = _walkable;
		worldPosition = _worldPos;
		gridX = _gridX;
		gridY = _gridY;
		movementPenalty = _penalty;
	}

	public int fCost {
		get {
			return gCost + hCost;
		}
	}

	public int HeapIndex {
		get {
			return heapIndex;
		}
		set {
			heapIndex = value;
		}
	}

    //Сравнение
	public int CompareTo(Node nodeToCompare) {
		int compare = fCost.CompareTo(nodeToCompare.fCost);
        //fCost = nodeToCompare.fCost
        if (compare == 0) {
			compare = hCost.CompareTo(nodeToCompare.hCost);
		}
		return -compare;
	}
}
