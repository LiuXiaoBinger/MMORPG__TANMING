using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"DOTween.dll",
		"Google.Protobuf.dll",
		"Network.dll",
		"System.Core.dll",
		"System.dll",
		"UniRx.dll",
		"Unity.InputSystem.dll",
		"UnityEngine.CoreModule.dll",
		"UnityEngine.JSONSerializeModule.dll",
		"YooAsset.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// DG.Tweening.Core.DOGetter<UnityEngine.Color>
	// DG.Tweening.Core.DOGetter<UnityEngine.Quaternion>
	// DG.Tweening.Core.DOGetter<UnityEngine.Vector2>
	// DG.Tweening.Core.DOGetter<UnityEngine.Vector3>
	// DG.Tweening.Core.DOGetter<float>
	// DG.Tweening.Core.DOGetter<int>
	// DG.Tweening.Core.DOGetter<object>
	// DG.Tweening.Core.DOSetter<UnityEngine.Color>
	// DG.Tweening.Core.DOSetter<UnityEngine.Quaternion>
	// DG.Tweening.Core.DOSetter<UnityEngine.Vector2>
	// DG.Tweening.Core.DOSetter<UnityEngine.Vector3>
	// DG.Tweening.Core.DOSetter<float>
	// DG.Tweening.Core.DOSetter<int>
	// DG.Tweening.Core.DOSetter<object>
	// DG.Tweening.Core.TweenerCore<UnityEngine.Quaternion,UnityEngine.Vector3,DG.Tweening.Plugins.Options.QuaternionOptions>
	// DG.Tweening.Core.TweenerCore<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>
	// DG.Tweening.Core.TweenerCore<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<DG.Tweening.Color2,DG.Tweening.Color2,DG.Tweening.Plugins.Options.ColorOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Color,UnityEngine.Color,DG.Tweening.Plugins.Options.ColorOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Quaternion,UnityEngine.Vector3,DG.Tweening.Plugins.Options.QuaternionOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Rect,UnityEngine.Rect,DG.Tweening.Plugins.Options.RectOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.Options.VectorOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector3,UnityEngine.Vector3,DG.Tweening.Plugins.Options.VectorOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.Vector3ArrayOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector4,UnityEngine.Vector4,DG.Tweening.Plugins.Options.VectorOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<double,double,DG.Tweening.Plugins.Options.NoOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<float,float,DG.Tweening.Plugins.Options.FloatOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<int,int,DG.Tweening.Plugins.Options.NoOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<long,long,DG.Tweening.Plugins.Options.NoOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<object,object,DG.Tweening.Plugins.Options.NoOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<object,object,DG.Tweening.Plugins.Options.StringOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<uint,uint,DG.Tweening.Plugins.Options.UintOptions>
	// DG.Tweening.Plugins.Core.ABSTweenPlugin<ulong,ulong,DG.Tweening.Plugins.Options.NoOptions>
	// Google.Protobuf.Collections.RepeatedField.<GetEnumerator>d__29<object>
	// Google.Protobuf.Collections.RepeatedField<object>
	// Google.Protobuf.FieldCodec.<>c<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass38_0<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass39_0<object>
	// Google.Protobuf.FieldCodec.InputMerger<object>
	// Google.Protobuf.FieldCodec.ValuesMerger<object>
	// Google.Protobuf.FieldCodec<object>
	// Google.Protobuf.IDeepCloneable<object>
	// Google.Protobuf.MessageParser.<>c__DisplayClass2_0<object>
	// Google.Protobuf.MessageParser<object>
	// Google.Protobuf.ValueReader<object>
	// Google.Protobuf.ValueWriter<object>
	// Singleton<object>
	// System.Action<DG.Tweening.Plugins.Options.PathOptions,object,UnityEngine.Quaternion,object>
	// System.Action<GhostEffect.MeshInfo>
	// System.Action<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Action<UnityEngine.CombineInstance>
	// System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>
	// System.Action<UnityEngine.SceneManagement.Scene>
	// System.Action<UnityEngine.Vector3>
	// System.Action<UnityEngine.Vector4>
	// System.Action<byte,int,object>
	// System.Action<byte>
	// System.Action<float>
	// System.Action<int,object>
	// System.Action<int>
	// System.Action<long>
	// System.Action<object,int>
	// System.Action<object,object,byte,byte>
	// System.Action<object,object,int>
	// System.Action<object,object,object,object,object>
	// System.Action<object,object>
	// System.Action<object>
	// System.Action<ushort>
	// System.Buffers.ArrayPool<int>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.LockedStack<int>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.PerCoreLockedStacks<int>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool<int>
	// System.ByReference<byte>
	// System.ByReference<int>
	// System.ByReference<object>
	// System.ByReference<ushort>
	// System.Collections.Generic.ArraySortHelper<GhostEffect.MeshInfo>
	// System.Collections.Generic.ArraySortHelper<HighlightPlus.HighlightEffect.ModelMaterials>
	// System.Collections.Generic.ArraySortHelper<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.CombineInstance>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.Vector3>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.Vector4>
	// System.Collections.Generic.ArraySortHelper<byte>
	// System.Collections.Generic.ArraySortHelper<float>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<GhostEffect.MeshInfo>
	// System.Collections.Generic.Comparer<HighlightPlus.HighlightEffect.ModelMaterials>
	// System.Collections.Generic.Comparer<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.Comparer<UnityEngine.CombineInstance>
	// System.Collections.Generic.Comparer<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.Comparer<UnityEngine.Vector3>
	// System.Collections.Generic.Comparer<UnityEngine.Vector4>
	// System.Collections.Generic.Comparer<byte>
	// System.Collections.Generic.Comparer<float>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<ActionStateKey,object>
	// System.Collections.Generic.Dictionary.Enumerator<UnityEngine.Vector3,int>
	// System.Collections.Generic.Dictionary.Enumerator<int,byte>
	// System.Collections.Generic.Dictionary.Enumerator<int,float>
	// System.Collections.Generic.Dictionary.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,ulong>
	// System.Collections.Generic.Dictionary.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,byte>
	// System.Collections.Generic.Dictionary.Enumerator<object,float>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<ulong,UnityEngine.Vector3>
	// System.Collections.Generic.Dictionary.Enumerator<ulong,object>
	// System.Collections.Generic.Dictionary.Enumerator<ulong,ulong>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<ActionStateKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<UnityEngine.Vector3,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,byte>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,float>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,ulong>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,byte>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,float>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<ulong,UnityEngine.Vector3>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<ulong,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<ulong,ulong>
	// System.Collections.Generic.Dictionary.KeyCollection<ActionStateKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection<UnityEngine.Vector3,int>
	// System.Collections.Generic.Dictionary.KeyCollection<int,byte>
	// System.Collections.Generic.Dictionary.KeyCollection<int,float>
	// System.Collections.Generic.Dictionary.KeyCollection<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,ulong>
	// System.Collections.Generic.Dictionary.KeyCollection<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,byte>
	// System.Collections.Generic.Dictionary.KeyCollection<object,float>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<ulong,UnityEngine.Vector3>
	// System.Collections.Generic.Dictionary.KeyCollection<ulong,object>
	// System.Collections.Generic.Dictionary.KeyCollection<ulong,ulong>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<ActionStateKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<UnityEngine.Vector3,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,byte>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,float>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,ulong>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,byte>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,float>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<ulong,UnityEngine.Vector3>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<ulong,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<ulong,ulong>
	// System.Collections.Generic.Dictionary.ValueCollection<ActionStateKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection<UnityEngine.Vector3,int>
	// System.Collections.Generic.Dictionary.ValueCollection<int,byte>
	// System.Collections.Generic.Dictionary.ValueCollection<int,float>
	// System.Collections.Generic.Dictionary.ValueCollection<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,ulong>
	// System.Collections.Generic.Dictionary.ValueCollection<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,byte>
	// System.Collections.Generic.Dictionary.ValueCollection<object,float>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<ulong,UnityEngine.Vector3>
	// System.Collections.Generic.Dictionary.ValueCollection<ulong,object>
	// System.Collections.Generic.Dictionary.ValueCollection<ulong,ulong>
	// System.Collections.Generic.Dictionary<ActionStateKey,object>
	// System.Collections.Generic.Dictionary<UnityEngine.Vector3,int>
	// System.Collections.Generic.Dictionary<int,byte>
	// System.Collections.Generic.Dictionary<int,float>
	// System.Collections.Generic.Dictionary<int,int>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<int,ulong>
	// System.Collections.Generic.Dictionary<long,int>
	// System.Collections.Generic.Dictionary<long,object>
	// System.Collections.Generic.Dictionary<object,byte>
	// System.Collections.Generic.Dictionary<object,float>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<ulong,UnityEngine.Vector3>
	// System.Collections.Generic.Dictionary<ulong,object>
	// System.Collections.Generic.Dictionary<ulong,ulong>
	// System.Collections.Generic.EqualityComparer<ActionStateKey>
	// System.Collections.Generic.EqualityComparer<UnityEngine.Vector3>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<float>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<long>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.EqualityComparer<ulong>
	// System.Collections.Generic.HashSet.Enumerator<int>
	// System.Collections.Generic.HashSet.Enumerator<long>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<int>
	// System.Collections.Generic.HashSet<long>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<int>
	// System.Collections.Generic.HashSetEqualityComparer<long>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<GhostEffect.MeshInfo>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<ActionStateKey,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<UnityEngine.Vector3,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,byte>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,float>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,ulong>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,byte>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<ulong,UnityEngine.Vector3>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<ulong,ulong>>
	// System.Collections.Generic.ICollection<UnityEngine.CombineInstance>
	// System.Collections.Generic.ICollection<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.ICollection<UnityEngine.Vector3>
	// System.Collections.Generic.ICollection<UnityEngine.Vector4>
	// System.Collections.Generic.ICollection<byte>
	// System.Collections.Generic.ICollection<float>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<long>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<GhostEffect.MeshInfo>
	// System.Collections.Generic.IComparer<HighlightPlus.HighlightEffect.ModelMaterials>
	// System.Collections.Generic.IComparer<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IComparer<UnityEngine.CombineInstance>
	// System.Collections.Generic.IComparer<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.IComparer<UnityEngine.Vector3>
	// System.Collections.Generic.IComparer<UnityEngine.Vector4>
	// System.Collections.Generic.IComparer<byte>
	// System.Collections.Generic.IComparer<float>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<GhostEffect.MeshInfo>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ActionStateKey,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<UnityEngine.Vector3,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,byte>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,float>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,ulong>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,byte>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ulong,UnityEngine.Vector3>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ulong,ulong>>
	// System.Collections.Generic.IEnumerable<UnityEngine.CombineInstance>
	// System.Collections.Generic.IEnumerable<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.IEnumerable<UnityEngine.Vector3>
	// System.Collections.Generic.IEnumerable<UnityEngine.Vector4>
	// System.Collections.Generic.IEnumerable<byte>
	// System.Collections.Generic.IEnumerable<float>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<long>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<GhostEffect.MeshInfo>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ActionStateKey,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<UnityEngine.Vector3,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,byte>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,float>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,ulong>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,byte>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ulong,UnityEngine.Vector3>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ulong,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ulong,ulong>>
	// System.Collections.Generic.IEnumerator<UnityEngine.CombineInstance>
	// System.Collections.Generic.IEnumerator<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.IEnumerator<UnityEngine.Vector3>
	// System.Collections.Generic.IEnumerator<UnityEngine.Vector4>
	// System.Collections.Generic.IEnumerator<byte>
	// System.Collections.Generic.IEnumerator<float>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<long>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<ActionStateKey>
	// System.Collections.Generic.IEqualityComparer<UnityEngine.Vector3>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<long>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IEqualityComparer<ulong>
	// System.Collections.Generic.IList<GhostEffect.MeshInfo>
	// System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IList<UnityEngine.CombineInstance>
	// System.Collections.Generic.IList<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.IList<UnityEngine.Vector3>
	// System.Collections.Generic.IList<UnityEngine.Vector4>
	// System.Collections.Generic.IList<byte>
	// System.Collections.Generic.IList<float>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.IReadOnlyCollection<int>
	// System.Collections.Generic.IReadOnlyCollection<object>
	// System.Collections.Generic.IReadOnlyDictionary<long,object>
	// System.Collections.Generic.IReadOnlyList<int>
	// System.Collections.Generic.IReadOnlyList<object>
	// System.Collections.Generic.KeyValuePair<ActionStateKey,object>
	// System.Collections.Generic.KeyValuePair<UnityEngine.Vector3,int>
	// System.Collections.Generic.KeyValuePair<int,byte>
	// System.Collections.Generic.KeyValuePair<int,float>
	// System.Collections.Generic.KeyValuePair<int,int>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<int,ulong>
	// System.Collections.Generic.KeyValuePair<long,int>
	// System.Collections.Generic.KeyValuePair<long,object>
	// System.Collections.Generic.KeyValuePair<object,byte>
	// System.Collections.Generic.KeyValuePair<object,float>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.KeyValuePair<ulong,UnityEngine.Vector3>
	// System.Collections.Generic.KeyValuePair<ulong,object>
	// System.Collections.Generic.KeyValuePair<ulong,ulong>
	// System.Collections.Generic.LinkedList.Enumerator<object>
	// System.Collections.Generic.LinkedList<object>
	// System.Collections.Generic.LinkedListNode<object>
	// System.Collections.Generic.List.Enumerator<GhostEffect.MeshInfo>
	// System.Collections.Generic.List.Enumerator<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.List.Enumerator<UnityEngine.CombineInstance>
	// System.Collections.Generic.List.Enumerator<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.List.Enumerator<UnityEngine.Vector3>
	// System.Collections.Generic.List.Enumerator<UnityEngine.Vector4>
	// System.Collections.Generic.List.Enumerator<byte>
	// System.Collections.Generic.List.Enumerator<float>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<GhostEffect.MeshInfo>
	// System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.List<UnityEngine.CombineInstance>
	// System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.List<UnityEngine.Vector3>
	// System.Collections.Generic.List<UnityEngine.Vector4>
	// System.Collections.Generic.List<byte>
	// System.Collections.Generic.List<float>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<GhostEffect.MeshInfo>
	// System.Collections.Generic.ObjectComparer<HighlightPlus.HighlightEffect.ModelMaterials>
	// System.Collections.Generic.ObjectComparer<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.ObjectComparer<UnityEngine.CombineInstance>
	// System.Collections.Generic.ObjectComparer<UnityEngine.SceneManagement.Scene>
	// System.Collections.Generic.ObjectComparer<UnityEngine.Vector3>
	// System.Collections.Generic.ObjectComparer<UnityEngine.Vector4>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<float>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<ActionStateKey>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.Vector3>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<float>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<long>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<ulong>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.Generic.ValueListBuilder<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<GhostEffect.MeshInfo>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.CombineInstance>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.SceneManagement.Scene>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.Vector3>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.Vector4>
	// System.Collections.ObjectModel.ReadOnlyCollection<byte>
	// System.Collections.ObjectModel.ReadOnlyCollection<float>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<GhostEffect.MeshInfo>
	// System.Comparison<HighlightPlus.HighlightEffect.ModelMaterials>
	// System.Comparison<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Comparison<UnityEngine.CombineInstance>
	// System.Comparison<UnityEngine.SceneManagement.Scene>
	// System.Comparison<UnityEngine.Vector3>
	// System.Comparison<UnityEngine.Vector4>
	// System.Comparison<byte>
	// System.Comparison<float>
	// System.Comparison<int>
	// System.Comparison<object>
	// System.Func<System.Threading.Tasks.VoidTaskResult>
	// System.Func<int,object,byte>
	// System.Func<object,System.Threading.Tasks.VoidTaskResult>
	// System.Func<object,byte>
	// System.Func<object,int>
	// System.Func<object>
	// System.Func<ulong,int,object>
	// System.IEquatable<ActionStateKey>
	// System.IObservable<long>
	// System.Nullable<UnityEngine.Vector3>
	// System.Nullable<int>
	// System.Predicate<GhostEffect.MeshInfo>
	// System.Predicate<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Predicate<UnityEngine.CombineInstance>
	// System.Predicate<UnityEngine.SceneManagement.Scene>
	// System.Predicate<UnityEngine.Vector3>
	// System.Predicate<UnityEngine.Vector4>
	// System.Predicate<byte>
	// System.Predicate<float>
	// System.Predicate<int>
	// System.Predicate<long>
	// System.Predicate<object>
	// System.ReadOnlySpan<byte>
	// System.ReadOnlySpan<int>
	// System.ReadOnlySpan<object>
	// System.ReadOnlySpan<ushort>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Span<byte>
	// System.Span<int>
	// System.Span<object>
	// System.Span<ushort>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.Task<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory<System.Threading.Tasks.VoidTaskResult>
	// System.Tuple<long,long>
	// UniRx.Observer.Subscribe<long>
	// UniRx.Observer.Subscribe_<long>
	// UnityEngine.Events.InvokableCall<UnityEngine.Vector2>
	// UnityEngine.Events.InvokableCall<object>
	// UnityEngine.Events.UnityAction<UnityEngine.Vector2>
	// UnityEngine.Events.UnityAction<object>
	// UnityEngine.Events.UnityEvent<UnityEngine.Vector2>
	// UnityEngine.Events.UnityEvent<object>
	// UnityEngine.InputSystem.InputBindingComposite<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputControl<float>
	// UnityEngine.InputSystem.InputProcessor<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputProcessor<float>
	// UnityEngine.InputSystem.Utilities.InlinedArray<object>
	// }}

	public void RefMethods()
	{
		// DG.Tweening.Core.TweenerCore<UnityEngine.Color,UnityEngine.Color,DG.Tweening.Plugins.Options.ColorOptions> DG.Tweening.Core.Extensions.Blendable<UnityEngine.Color,UnityEngine.Color,DG.Tweening.Plugins.Options.ColorOptions>(DG.Tweening.Core.TweenerCore<UnityEngine.Color,UnityEngine.Color,DG.Tweening.Plugins.Options.ColorOptions>)
		// object DG.Tweening.Core.Extensions.SetSpecialStartupMode<object>(object,DG.Tweening.Core.Enums.SpecialStartupMode)
		// DG.Tweening.Core.TweenerCore<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions> DG.Tweening.Core.TweenManager.GetTweener<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>()
		// DG.Tweening.Core.TweenerCore<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions> DG.Tweening.Core.TweenManager.GetTweener<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>()
		// DG.Tweening.Core.TweenerCore<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions> DG.Tweening.DOTween.ApplyTo<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>(DG.Tweening.Core.DOGetter<UnityEngine.Vector2>,DG.Tweening.Core.DOSetter<UnityEngine.Vector2>,UnityEngine.Vector2,float,DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>)
		// DG.Tweening.Core.TweenerCore<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions> DG.Tweening.DOTween.ApplyTo<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>(DG.Tweening.Core.DOGetter<UnityEngine.Vector3>,DG.Tweening.Core.DOSetter<UnityEngine.Vector3>,object,float,DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>)
		// DG.Tweening.Core.TweenerCore<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions> DG.Tweening.DOTween.To<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>(DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>,DG.Tweening.Core.DOGetter<UnityEngine.Vector2>,DG.Tweening.Core.DOSetter<UnityEngine.Vector2>,UnityEngine.Vector2,float)
		// DG.Tweening.Core.TweenerCore<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions> DG.Tweening.DOTween.To<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>(DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>,DG.Tweening.Core.DOGetter<UnityEngine.Vector3>,DG.Tweening.Core.DOSetter<UnityEngine.Vector3>,object,float)
		// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions> DG.Tweening.Plugins.Core.PluginsManager.GetDefaultPlugin<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>()
		// DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions> DG.Tweening.Plugins.Core.PluginsManager.GetDefaultPlugin<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>()
		// object DG.Tweening.TweenSettingsExtensions.OnStart<object>(object,DG.Tweening.TweenCallback)
		// object DG.Tweening.TweenSettingsExtensions.OnUpdate<object>(object,DG.Tweening.TweenCallback)
		// object DG.Tweening.TweenSettingsExtensions.SetEase<object>(object,DG.Tweening.Ease)
		// object DG.Tweening.TweenSettingsExtensions.SetEase<object>(object,UnityEngine.AnimationCurve)
		// object DG.Tweening.TweenSettingsExtensions.SetLoops<object>(object,int,DG.Tweening.LoopType)
		// object DG.Tweening.TweenSettingsExtensions.SetRelative<object>(object)
		// object DG.Tweening.TweenSettingsExtensions.SetTarget<object>(object,object)
		// object DG.Tweening.TweenSettingsExtensions.SetUpdate<object>(object,DG.Tweening.UpdateType)
		// bool DG.Tweening.Tweener.Setup<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>(DG.Tweening.Core.TweenerCore<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>,DG.Tweening.Core.DOGetter<UnityEngine.Vector2>,DG.Tweening.Core.DOSetter<UnityEngine.Vector2>,UnityEngine.Vector2,float,DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector2,UnityEngine.Vector2,DG.Tweening.Plugins.CircleOptions>)
		// bool DG.Tweening.Tweener.Setup<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>(DG.Tweening.Core.TweenerCore<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>,DG.Tweening.Core.DOGetter<UnityEngine.Vector3>,DG.Tweening.Core.DOSetter<UnityEngine.Vector3>,object,float,DG.Tweening.Plugins.Core.ABSTweenPlugin<UnityEngine.Vector3,object,DG.Tweening.Plugins.Options.PathOptions>)
		// object System.Activator.CreateInstance<object>()
		// object[] System.Array.Empty<object>()
		// System.Void System.Array.Resize<UnityEngine.CombineInstance>(UnityEngine.CombineInstance[]&,int)
		// System.Void System.Array.Sort<HighlightPlus.HighlightEffect.ModelMaterials>(HighlightPlus.HighlightEffect.ModelMaterials[],System.Comparison<HighlightPlus.HighlightEffect.ModelMaterials>)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForCompletion>d__10>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForCompletion>d__10&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForElapsedLoops>d__13>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForElapsedLoops>d__13&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForKill>d__12>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForKill>d__12&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForPosition>d__14>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForPosition>d__14&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForRewind>d__11>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForRewind>d__11&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForStart>d__15>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForStart>d__15&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForCompletion>d__10>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForCompletion>d__10&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForElapsedLoops>d__13>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForElapsedLoops>d__13&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForKill>d__12>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForKill>d__12&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForPosition>d__14>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForPosition>d__14&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForRewind>d__11>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForRewind>d__11&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForStart>d__15>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter&,DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForStart>d__15&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForCompletion>d__10>(DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForCompletion>d__10&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForElapsedLoops>d__13>(DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForElapsedLoops>d__13&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForKill>d__12>(DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForKill>d__12&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForPosition>d__14>(DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForPosition>d__14&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForRewind>d__11>(DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForRewind>d__11&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForStart>d__15>(DG.Tweening.DOTweenModuleUnityVersion.<AsyncWaitForStart>d__15&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// System.Tuple<long,long> System.Tuple.Create<long,long>(long,long)
		// System.IDisposable UniRx.ObservableExtensions.Subscribe<long>(System.IObservable<long>,System.Action<long>)
		// System.IObserver<long> UniRx.Observer.CreateSubscribeObserver<long>(System.Action<long>,System.Action<System.Exception>,System.Action)
		// System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf<UnityEngine.Vector2>(UnityEngine.Vector2&)
		// int Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<UnityEngine.Vector2>()
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.Component.GetComponentInChildren<object>()
		// object UnityEngine.Component.GetComponentInChildren<object>(bool)
		// object UnityEngine.Component.GetComponentInParent<object>()
		// System.Void UnityEngine.Component.GetComponentsInChildren<object>(System.Collections.Generic.List<object>)
		// System.Void UnityEngine.Component.GetComponentsInChildren<object>(bool,System.Collections.Generic.List<object>)
		// object[] UnityEngine.Component.GetComponentsInChildren<object>()
		// object[] UnityEngine.Component.GetComponentsInChildren<object>(bool)
		// bool UnityEngine.Component.TryGetComponent<object>(object&)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>(bool)
		// System.Void UnityEngine.GameObject.GetComponentsInChildren<object>(System.Collections.Generic.List<object>)
		// System.Void UnityEngine.GameObject.GetComponentsInChildren<object>(bool,System.Collections.Generic.List<object>)
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
		// bool UnityEngine.GameObject.TryGetComponent<object>(object&)
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputAction.ReadValue<UnityEngine.Vector2>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputAction.CallbackContext.ReadValue<UnityEngine.Vector2>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ApplyProcessors<UnityEngine.Vector2>(int,UnityEngine.Vector2,UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>)
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ReadValue<UnityEngine.Vector2>(int,int,bool)
		// object UnityEngine.JsonUtility.FromJson<object>(string)
		// object UnityEngine.Object.FindAnyObjectByType<object>(UnityEngine.FindObjectsInactive)
		// object UnityEngine.Object.FindObjectOfType<object>(bool)
		// object[] UnityEngine.Object.FindObjectsByType<object>(UnityEngine.FindObjectsInactive,UnityEngine.FindObjectsSortMode)
		// object UnityEngine.Object.Instantiate<object>(object)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform,bool)
		// object[] UnityEngine.Resources.ConvertObjects<object>(UnityEngine.Object[])
		// object UnityEngine.Resources.GetBuiltinResource<object>(string)
		// object UnityEngine.Resources.Load<object>(string)
		// object UnityEngine.ScriptableObject.CreateInstance<object>()
		// object YooAsset.AssetOperationHandle.GetAssetObject<object>()
		// YooAsset.AssetOperationHandle YooAsset.ResourcePackage.LoadAssetAsync<object>(string)
		// YooAsset.SubAssetsOperationHandle YooAsset.ResourcePackage.LoadSubAssetsAsync<object>(string)
		// object YooAsset.SubAssetsOperationHandle.GetSubAssetObject<object>(string)
	}
}