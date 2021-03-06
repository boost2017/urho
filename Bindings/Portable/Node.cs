//
// Node C# sugar
//
// Authors:
//   Miguel de Icaza (miguel@xamarin.com)
//
// Copyrigh 2015 Xamarin INc
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Urho {

	internal partial class NodeHelper {
		[DllImport (Consts.NativeImport, CallingConvention=CallingConvention.Cdecl)]
		internal extern static IntPtr urho_node_get_components(IntPtr node, int code, int recursive, out int count);
	}
	
	public partial class Node {
		static Node[] ZeroArray = new Node[0];
			
		public Node[] GetChildrenWithComponent<T> (bool recursive = false) where T: Component
		{
			Runtime.ValidateRefCounted(this);
			var stringhash = Runtime.LookupStringHash (typeof (T));
			int count;
			var ptr = NodeHelper.urho_node_get_components (handle, stringhash.Code, recursive ? 1 : 0, out count);
			if (ptr == IntPtr.Zero)
				return ZeroArray;
			
			var res = new Node[count];
			for (int i = 0; i < count; i++){
				var node = Marshal.ReadIntPtr(ptr, i * IntPtr.Size);
				res [i] = Runtime.LookupObject<Node> (node);
			}
			if (Component.IsDefinedInManagedCode<T>())
				//is not really efficient, but underlying Urho3D knows nothing about components defined in C#
				return res.Where(c => c.GetComponent<T>() != null).ToArray();
			return res;
		}
		
		public T CreateComponent<T> (StringHash type, CreateMode mode = CreateMode.Replicated, uint id = 0) where T:Component
		{
			Runtime.ValidateRefCounted(this);
			var ptr = Node_CreateComponent (handle, type.Code, mode, id);
			return Runtime.LookupObject<T> (ptr);
		}

		public void RemoveComponent<T> ()
		{
			Runtime.ValidateRefCounted(this);
			var stringHash = Runtime.LookupStringHash (typeof (T));
			RemoveComponent (stringHash);
		}

		public T CreateComponent<T> (CreateMode mode = CreateMode.Replicated, uint id = 0) where T:Component
		{
			Runtime.ValidateRefCounted(this);
			var component = Activator.CreateInstance<T>();
			AddComponent(component, id, mode);
			return component;
		}

		/// <summary>
		/// Add a pre-created component.
		/// </summary>
		public void AddComponent (Component component, uint id = 0)
		{
			Runtime.ValidateRefCounted(this);
			AddComponent (component, id, CreateMode.Replicated);
		}
		
		/// <summary>
		/// Changes Parent for the node
		/// </summary>
		public void ChangeParent(Node newParent)
		{
			AddRef();
			Remove(); //without AddRef "Delete" will completly delete the node and the next operation will throw AccessViolationException
			newParent.AddChild(this);
			ReleaseRef();
		}

		public T GetComponent<T> (bool recursive = false) where T : Component
		{
			Runtime.ValidateRefCounted(this);
			return (T)Components.FirstOrDefault(c => c is T);
		}

		public T GetOrCreateComponent<T>(bool recursive = false) where T : Component
		{
			Runtime.ValidateRefCounted(this);
			var component = (T)Components.FirstOrDefault(c => c is T);
			if (component == null)
				return CreateComponent<T>();
			return component;
		}

		public bool LoadXml(string prefab)
		{
			var file = Application.Current.ResourceCache.GetXmlFile(prefab, true);
			var element = file.GetRoot("node");
			if (element == null || element.Null)
				throw new Exception("'node' root element was not found");
			return LoadXml(element, true);
		}
	}
}
