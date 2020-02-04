/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

using System;
using IonDotnet.Tree;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonBlobTest : IonLobTest
    {
        protected override IIonValue MakeMutableValue()
        {
            return new IonBlob(new byte[0]);
        }

        protected override IIonValue MakeNullValue()
        {
            return IonBlob.NewNull();
        }

        protected override IIonValue MakeWithBytes(ReadOnlySpan<byte> bytes)
        {
            return new IonBlob(bytes);
        }

        protected override IonType MainIonType => IonType.Blob;
    }
}
