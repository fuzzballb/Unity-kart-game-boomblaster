using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// http://www.blackpawn.com/texts/lightmaps/
public class Node<T> where T : class
{
	private Node<T>[] children;

	public Rect Rectangle;

	public T Element = default(T);

	public Node(Rect rectangle)
	{
		Rectangle = rectangle;
	}

	public bool IsLeaf { get { return children == null; } }

	public Rect PackedRectangle
	{
		get
		{
			var stack = new Stack<Node<T>>();
			stack.Push(this);

			var xMax = 0.0f;
			var yMax = 0.0f;
				
			while (stack.Count > 0)
			{
				var node = stack.Pop();
				if (!node.IsLeaf)
				{
					stack.Push(node.children[0]);
					stack.Push(node.children[1]);
				}
				else if (node.Element != null)
				{
					xMax = Mathf.Max(xMax, node.Rectangle.xMax);
					yMax = Mathf.Max(yMax, node.Rectangle.yMax);
				}
			}

			return new Rect(Rectangle.xMin, Rectangle.yMin, xMax - Rectangle.xMin, yMax - Rectangle.yMin);
		}
	}

	public IEnumerable<Node<T>> Leafs
	{
		get
		{
			if (IsLeaf)
				yield return this;
			else
			{
				foreach (var leaf in children[0].Leafs)
					yield return leaf;
				foreach (var leaf in children[1].Leafs)
					yield return leaf;
			}
		}
	}

	public Node<T> Insert(T element, Vector2 size)
	{
		var node = InserToSelf(size);
		if (node != null)
			node.Element = element;
		return node;
	}

	private Node<T> InserToSelf(Vector2 size)
	{
		if (!IsLeaf)
			return InsertToChildren(ref size);
		else
		{
			if (!IsSuitable(ref size))
				return null;

			if (FitsPerfectly(ref size))
				return this;

			return InsertToLeaf(ref size);
		}
	}

	private Node<T> InsertToLeaf(ref Vector2 size)
	{
		if (ShouldSplitHorizontal(ref size))
			SplitHorizontal(size);
		else
			SplitVertical(size);

		return children[0].InserToSelf(size);
	}

	private Node<T> InsertToChildren(ref Vector2 size)
	{
		var newNode = children[0].InserToSelf(size);
		if (newNode != null)
			return newNode;

		return children[1].InserToSelf(size);
	}

	private void SplitVertical(Vector2 size)
	{
		children = new Node<T>[]
		{
			new Node<T>(new Rect(Rectangle.xMin, Rectangle.yMin, Rectangle.width, size.y)),
				new Node<T>(new Rect(Rectangle.xMin, Rectangle.yMin + size.y, Rectangle.width, Rectangle.height - size.y))
		};
	}

	private void SplitHorizontal(Vector2 size)
	{
		children = new Node<T>[]
		{
			new Node<T>(new Rect(Rectangle.xMin, Rectangle.yMin, size.x, Rectangle.height)),
			new Node<T>(new Rect(Rectangle.xMin + size.x, Rectangle.yMin, Rectangle.width - size.x, Rectangle.height))
		};
	}

	private bool IsSuitable(ref Vector2 size)
	{
		return Element == null && Rectangle.width >= size.x && Rectangle.height >= size.y;
	}

	private bool FitsPerfectly(ref Vector2 size)
	{
		return Rectangle.width == size.x && Rectangle.height == size.y;
	}

	private bool ShouldSplitHorizontal(ref Vector2 size)
	{
		return Rectangle.width - size.x > Rectangle.height - size.y;
	}
}
