using UnityEngine;
using UnityEditor;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityTest
{
	[TestFixture]
	internal class PromoTests
	{
		[Test]
		public void Result_Difficulty() {
            Promo p = ScriptableObject.CreateInstance<Promo>();

            p.difficulty = 1;
            float creativity = 10;

            float result = p.CalculateResult(creativity);

            p.difficulty = 2;
            Assert.IsTrue(result > p.CalculateResult(creativity));

            p.difficulty = 0.5f;
            Assert.IsTrue(result < p.CalculateResult(creativity));
		}
    }
}