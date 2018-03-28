using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class PlayModeTests
{

	[Test]
	public void MapboxTestsWontShowHereButAreExcuted_RunPlayerToSeeTheirResults()
	{
		// Use the Assert class to test conditions.
	}

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	//[UnityTest]
	//public IEnumerator NewPlayModeTestWithEnumeratorPasses()
	//{
	//	Assert.AreEqual(1, 1);
	//	yield return null;
	//}
}
