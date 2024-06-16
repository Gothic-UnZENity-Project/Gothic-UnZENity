using System.Collections;
using UnityEngine;

namespace GUZ.Core
{
	public interface CoroutineManager
	{
		public Coroutine StartCoroutine(IEnumerator routine);
		public void StopCoroutine(Coroutine obj);
	}
}