// 
// Gendarme.Rules.Serialization.MissingSerializationConstructorRule
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

using Mono.Cecil;

using Gendarme.Framework;
using Gendarme.Framework.Helpers;
using Gendarme.Framework.Rocks;

namespace Gendarme.Rules.Serialization {

	[Problem ("The method has the wrong signature, it should return System.Void and have a single parameter of type 'System.Runtime.Serialization.StreamingContext' and be private.")]
	[Solution ("Fix method signature to match the runtime requirements.")]
	public class UseCorrectSignatureForSerializationMethodsRule : Rule, IMethodRule {

		private const string NotSerializableText = "The type of this method is not marked as [Serializable].";
		private const string WrongSignatureText = "The method has the wrong signature, it should return System.Void and have a single parameter of type 'System.Runtime.Serialization.StreamingContext' and be private.";

		static string [] Attributes = {
			"System.Runtime.Serialization.OnSerializingAttribute",
			"System.Runtime.Serialization.OnSerializedAttribute",
			"System.Runtime.Serialization.OnDeserializingAttribute",
			"System.Runtime.Serialization.OnDeserializedAttribute"
		};

		public override void Initialize (IRunner runner)
		{
			base.Initialize (runner);

			// the attributes are only available since fx 2.0 so there's no point
			// to execute it on every methods if the assembly target runtime is
			// earlier than 2.0
			Runner.AnalyzeAssembly += delegate (object o, RunnerEventArgs e) {
				Active = (e.CurrentAssembly.Runtime >= TargetRuntime.NET_2_0);
			};
		}

		public RuleResult CheckMethod (MethodDefinition method)
		{
			// rule does not apply to constructor or to methods without custom attributes
			if (method.IsConstructor || (method.CustomAttributes.Count == 0))
				return RuleResult.DoesNotApply;

			// marked with any of On[Des|S]erializ[ed|ing]Attribute ?
			if (!method.CustomAttributes.ContainsAnyType (Attributes))
				return RuleResult.DoesNotApply;

			// rule apply!

			// if the type is not marked as [Serializable] then warn that this code is useless
			bool serializable = (method.DeclaringType as TypeDefinition).IsSerializable;
			if (!serializable)
				Runner.Report (method, Severity.Critical, Confidence.Total, NotSerializableText);

			// check if the method signature is correct, return if it is
			if (MethodSignatures.SerializationEventHandler.Matches (method))
				return Runner.CurrentRuleResult;

			// but report an error if the signature isn't valid
			Runner.Report (method, Severity.Critical, Confidence.Total, WrongSignatureText);
			return RuleResult.Failure;
		}
	}
}
