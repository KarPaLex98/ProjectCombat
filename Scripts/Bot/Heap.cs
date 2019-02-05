using UnityEngine;
using System.Collections;
using System;

public class Heap<T> where T : IHeapItem<T> {
	
	T[] items;
	int currentItemCount; //количество элементов
	
      //Max heap size = gridSizeX*gridSizeY
	public Heap(int maxHeapSize) {
		items = new T[maxHeapSize];
	}
	
	public void Add(T item) {
		item.HeapIndex = currentItemCount;
		items[currentItemCount] = item; //Добавляем в конец
		SortUp(item);
		currentItemCount++;
	}
    
    //удаляем первый элемент
	public T RemoveFirst() {
        T firstItem = items[0];
		currentItemCount--;
		items[0] = items[currentItemCount];
		items[0].HeapIndex = 0;
		SortDown(items[0]);
		return firstItem;
	}

   
	public void UpdateItem(T item) {
		SortUp(item);
	}

    //Получаем текущее количество элементов в куче
	public int Count {
		get {
			return currentItemCount;
		}
	}

    //Проверяем содержит ли куча этот элемент
	public bool Contains(T item) {
		return Equals(items[item.HeapIndex], item);
	}

    //Сортировать вниз
	void SortDown(T item) {
		while (true) {
			int childIndexLeft = item.HeapIndex * 2 + 1; //левый ребенок
			int childIndexRight = item.HeapIndex * 2 + 2; //Правый ребенок
			int swapIndex = 0;

            //Дочерний левый индекс меньше текущего
			if (childIndexLeft < currentItemCount) {
				swapIndex = childIndexLeft;

                //Дочерний правый индекс меньше текущего
                if (childIndexRight < currentItemCount) { 
					if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0) {
						swapIndex = childIndexRight;
					}
				}

                //Сравниваем приоритеты  (должно быть меньше)
                if (item.CompareTo(items[swapIndex]) < 0) {
					Swap (item,items[swapIndex]);
				}
				else {
					return;
				}

			}
			else {
				return;
			}

		}
	}
	
	void SortUp(T item) {
		int parentIndex = (item.HeapIndex-1)/2; //индекс родителя
		
		while (true) {
			T parentItem = items[parentIndex]; //Родительский элемент
            //Сравниваем текущий с родительсим
            //1 если приоритет выше, 0 если одинаковый и -1 если ниже
            if (item.CompareTo(parentItem) > 0) {
				Swap (item,parentItem);
			}
            //Выходим, если нет больше младших родителей
			else {
				break;
			}

			parentIndex = (item.HeapIndex-1)/2;
		}
	}
	
	void Swap(T itemA, T itemB) {
        //Меняем содержимое
		items[itemA.HeapIndex] = itemB;
		items[itemB.HeapIndex] = itemA;
        //Меняем индекс
		int itemAIndex = itemA.HeapIndex;
		itemA.HeapIndex = itemB.HeapIndex;
		itemB.HeapIndex = itemAIndex;
	}
}

//
public interface IHeapItem<T> : IComparable<T> {
	int HeapIndex {
		get;
		set;
	}
}