﻿/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System.Collections.Generic;
using System.Numerics;
using Amazon.IonDotnet.Tests.Common;

namespace Amazon.IonDotnet.Tests.Internals
{
    using Amazon.IonDotnet.Builders;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BigIntegerTest
    {
        [TestMethod]
        public void TestLargeIntsWithHighOrderBitSet()
        {
            var largeInt3 =
                "10386193132828768511650942041097946556528556280326294976" +
                "830716527374613866266053821548081910442478515448889232458" +
                "686441609837892283981510185420164452852913701054817415485" +
                "917964960608946898600784984440767589149475212892713317317" +
                "788635808868058448157426059485484474034135892771614482794" +
                "643081563770686772491835340730475363855599331227685990921" +
                "818875771917092764284082356075008598818748845560405142558" +
                "998474389141860145018893606112625090956188859225017501251" +
                "491910628876068478502523392173793457667555946530350343421" +
                "525599975410367689165661853099910783975258109257009655117" +
                "191104674955980090016595222078269079251405233288240024070" +
                "283549176444544724347377243322578208435414140549340423359" +
                "177037924195351396505197864426864585179649280812108113958" +
                "334079073694815461263242760874118816267817696548243964691" +
                "828404769120386952638346282964432212444815100381827588684" +
                "123213501390168516360245854982243060089497918076475520091" +
                "100296631305885093957411063054280496305069080583815114600" +
                "487398023488573479515316046917417334254052243415738507487" +
                "842706347476696757993729956585660058743634169339298140299" +
                "689603025365105800567124176085364969870175965162165765759" +
                "768935915035948575971490093181913369811016966489562069975" +
                "05147919847908049246926031885699829927";

            var tests = new Dictionary<string, string>
            {
                {"good/equivs/intsLargeNegative1.10n", "-18344837831112429282"},
                {"good/equivs/intsLargePositive1.10n",  "18344837831112429282"},
                {"good/equivs/intsLargeNegative2.10n", "-4696278484764781896429"},
                {"good/equivs/intsLargePositive2.10n",  "4696278484764781896429"},
                {"good/equivs/intsLargeNegative3.10n", "-" + largeInt3},
                {"good/equivs/intsLargePositive3.10n",       largeInt3},
            };

            foreach (var entry in tests)
            {
                var expected = BigInteger.Parse(entry.Value);
                var bytes = DirStructure.IonTestFileAsBytes(entry.Key);
                var reader = IonReaderBuilder.Build(bytes);
                try
                {
                    reader.MoveNext();
                    reader.StepIn();
                    reader.MoveNext();
                    Assert.AreEqual(expected, reader.BigIntegerValue());
                    reader.MoveNext();
                    Assert.AreEqual(expected, reader.BigIntegerValue());
                    reader.StepOut();
                }
                finally
                {
                    reader.Dispose();
                }
            }
        }
    }
}

