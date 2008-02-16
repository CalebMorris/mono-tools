// 
// Gendarme.Rules.Serialization.DeserializeOptionalFieldRule
//
// Authors:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;

using Gendarme.Framework;
using Gendarme.Framework.Rocks;

namespace Gendarme.Rules.Serialization {

	[Problem ("Some fields are marked with [OptionalField] but the type does not provide special deserialization routines.")]
	[Solution ("Add a deserialization routine, marked with [OnDeserialized], and re-compute the correct value for the optional fields.")]
	public class DeserializeOptionalFieldRule : Rule, ITypeRule {

		private const string MessageOptional = "Optional fields '{0}' is not deserialized.";
		private const string MessageSerializable = "Optional fields '{0}' in non-serializable type.";

		private const string OptionalFieldAttribute = "System.Runtime.Serialization.OptionalFieldAttribute";
		private const string OnDeserializedAttribute = "System.Runtime.Serialization.OnDeserializedAttribute";
		private const string OnDeserializingAttribute = "System.Runtime.Serialization.OnDeserializingAttribute";

		public override void Initialize (IRunner runner)
		{
			base.Initialize (runner);

			// the [OptionalField] and deserialization attributes are only available 
			// since fx 2.0 so there's no point to execute it on every methods if the 
			// assembly target runtime is earlier than 2.0
			Runner.AnalyzeAssembly += delegate (object o, RunnerEventArgs e) {
				Active = (e.CurrentAssembly.Runtime >= TargetRuntime.NET_2_0);
			};
		}

		public RuleResult CheckType (TypeDefinition type)
		{
			// note: we cannot quickly return DoesNotApply since we would miss cases
			// where [OptionalField] is used on an non-[Serializable] type

			// look in methods for a deserialization candidates
			bool deserialized_candidate = false;
			bool deserializing_candidate = false;
			foreach (MethodDefinition method in type.Methods) {
				if (method.CustomAttributes.ContainsType (OnDeserializedAttribute))
					deserialized_candidate = true;
				if (method.CustomAttributes.ContainsType (OnDeserializingAttribute))
					deserializing_candidate = true;
				if (deserialized_candidate && deserializing_candidate)
					break;
			}

			// check if we found some optional fields, if none then it's all ok
			foreach (FieldDefinition field in type.Fields) {
				if (field.CustomAttributes.ContainsType (OptionalFieldAttribute)) {
					if (type.IsSerializable) {
						// report if we didn't find a deserialization method
						if (!deserialized_candidate || !deserializing_candidate) {
							// Medium since it's possible that the optional fields don't need to be re-computed
							string s = String.Format (MessageOptional, field.Name);
							Runner.Report (field, Severity.Medium, Confidence.High, s);
						}
					} else {
						// [OptionalField] without [Serializable] is a bigger problem
						string s = String.Format (MessageSerializable, field.Name);
						Runner.Report (field, Severity.Critical, Confidence.High, s);
					}
				}
			}

			return Runner.CurrentRuleResult;
		}
	}
}
