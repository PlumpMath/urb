using System;                    
using System.Reflection;         
using System.Collections.Generic;

namespace Urb
{
    public partial class ULisp
    {

        #region Type Inferfence              

        private static Dictionary<string, Token> _typeInference
            (Dictionary<string, Token> _parameters, Block _body, string _functionName)
        {
            var counter = 0;
            var _dict = new Dictionary<string, Token>();
            var _l = new List<string>();
            foreach (string name in _parameters.Keys)
            {
                _l.Add(name);
            }
            var _revl = new List<string>();
            for (int i = _l.Count - 1; i > -1; i--)
            {
                _revl.Add(_l[i]);
            }
            foreach (string name in _revl)
            {
                /// search for the name.
                _parameterDict.Add(name, new PInfo() { isFunction = counter == 0 });
                var _type = _parameters[name].type;
                _type = _searchFor(name, _body, _functionName);
                if (_type == null)
                {
                    if (_parameterDict[name].equalTypeNeighbour != null)
                        _print("Wait for {0}.\n\n", _parameterDict[name].equalTypeNeighbour);
                }
                else
                {
                    _dict.Add(name, new Token(name, _type));
                    _parameterDict[name].exactType = _type;
                    _parameterDict[name].isVerified = true;
                    Console.WriteLine("got {0} for {1}.\n", _type, name);
                }
                counter++;
            }
            /// Linking...
            _dict = _refreshInference(_dict);
            /// Wonder if all parameters are inferred:
            if (_dict.Count < _parameterDict.Count)
            {
                foreach (var _parameter in _parameterDict)
                {
                    if (!_dict.ContainsKey(_parameter.Key) &&
                        _parameterDict[_parameter.Key].exactType != null)
                    {
                        var _pair = new Token(_parameter.Key, _parameter.Value.exactType);
                        _dict.Add(_parameter.Key, _pair);
                    }
                }
                //throw new NotImplementedException();
            }
            /// Flush.
            _parameterDict.Clear();
            return _dict;
        }

        private static Dictionary<string, Token> _refreshInference(Dictionary<string, Token> _dict)
        {
            foreach (var lv1_p in _parameterDict)
            {
                foreach (var p in _parameterDict)
                {
                    if (!p.Value.isVerified)
                    {
                        /// Update _dict till done.
                        _dict = _linkInference(_dict);
                    }
                }
            }
            return _dict;
        }

        private static Dictionary<string, Token> _linkInference(Dictionary<string, Token> _dict)
        {
            foreach (var p in _parameterDict)
            {
                if (!p.Value.isVerified)
                {
                    if (p.Value.equalTypeNeighbour != null)
                    {
                        if (_parameterDict[p.Value.equalTypeNeighbour].isVerified)
                        {
                            p.Value.exactType = _parameterDict[p.Value.equalTypeNeighbour].exactType;
                            p.Value.isVerified = true;
                            _dict.Add(p.Key, new Token(p.Key, p.Value.exactType));
                            _print("linked {2} from {1} -> {0}.\n",
                                p.Key, p.Value.equalTypeNeighbour, p.Value.exactType);
                        }
                        else
                        {
                            /// ?
                        }
                    }
                }
            }
            return _dict;
        }

        private static List<string> _allReferencesCache()
        {
            /// need caching whole namespace things !
            var _dict = new List<string>();
            foreach (var _reference in _loadedReferences)
            {
                var _asm = Assembly.Load(_reference);
                var _types = _asm.GetExportedTypes();
                foreach (var t in _types)
                {
                    //_print("\n{0}", t.FullName);
                    _dict.Add(t.FullName);
                    foreach (var member in t.GetMembers(BindingFlags.Public))
                    {
                        _dict.Add(member.Name);
                    }
                }
            }
            return _dict;
        }

        private static HashList<string> _findPossibleParameterTypes
        (string methodName, List<string> possibleClasses, object[] currentExpression, int position)
        {
            var _typeCandidates = new HashList<string>();

            foreach (var _candidate in possibleClasses)
            {
                _print("\nfor candidate: {0}", _candidate);

                var _class = Type.GetType(_candidate);
                var _methods = _class.GetMethods();

                /// caching methods to find
                foreach (var _method in _methods)
                {
                    if (_method.Name == methodName)
                    {
                        var _parameters = _method.GetParameters();
                        if (_parameters.Length == currentExpression.Length - 1)
                        {
                            Console.Write("\n  {0} : ", methodName);
                            foreach (var _parameter in _parameters)
                            {
                                Console.Write("{0} ", _parameter.ParameterType.Name);
                            }
                            _typeCandidates.AddUnique(_parameters[position - 1].ParameterType.Name);

                        }
                    }
                }
                _print("Got candidates:\n");
                foreach (var _typeCandi in _typeCandidates)
                    _print("{0}\n", _typeCandi);

            }
            return _typeCandidates;
        }

        private static string[] _splitMethodNameAndClass(string functionInvoker)
        {
            /// mean it's from .NET:
            /// break into -> _method + _ns
            var _lastIndex = functionInvoker.LastIndexOf(".");
            var _methodName = functionInvoker.Substring(_lastIndex + 1);
            var _className = functionInvoker.Replace("." + _methodName, "");
            _print("broken into: {0} : {1}", _className, _methodName);
            return new string[]
            {
                _methodName,
                _className
            };
        }

        private static HashList<string> _findFullNameClasses(string functionInvoker)
        {
            /// Get current references cache:
            var _dict = _allReferencesCache();
            _print("\ncached all using references.\n");

            /// Get method name + path.
            var _methodClassName = _splitMethodNameAndClass(functionInvoker);
            var _methodName = _methodClassName[0];
            var _className = _methodClassName[1];

            /// Get all possible candidate CLASS by using namespace:
            var _classCandidates = new HashList<string>();
            foreach (var _namespace in _usingNamespaces)
            {
                var _a = _namespace + "." + _className;

                if (_dict.Contains(_a))
                {
                    _print("\ncandidate: {0}", _a);
                    _classCandidates.AddUnique(_a);
                }
            }
            return _classCandidates;
        }

        private static List<MethodInfo> _findMethods(HashList<string> possibleClassNames)
        {
            var _methodInfos = new List<MethodInfo>();
            /// seeking...
            foreach (var _candidate in possibleClassNames)
            {
                _print("\nfor candidate: {0}", _candidate);

                var _class = Type.GetType(_candidate);
                var _methods = _class.GetMethods();
                foreach (var method in _methods)
                    _methodInfos.Add(method);
            }
            return _methodInfos;
        }

        private static List<MethodInfo> _findMethodOverload(string functionInvoker, object[] parameters)
        {
            var _methodName = _splitMethodNameAndClass(functionInvoker)[0];

            var _classCandidates = _findFullNameClasses(functionInvoker);

            var _methodInfos = _findMethods(_classCandidates);

            var _methodCandidates = new List<MethodInfo>();

            foreach (var methodInfo in _methodInfos)
            {
                if (methodInfo.Name == _methodName)
                {
                    /// Fetch parameter infos:
                    var methodParams = methodInfo.GetParameters();
                    /// checking on p number:
                    if (methodParams.Length == parameters.Length)
                    {
                        /// matching parameters:
                        bool isMatched = false;
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            /// At this state, only Token and Block case:
                            if (parameters[i] is Token)
                            {
                                var _paramToken = parameters[i] as Token;
                                /// when it's token, only 2 cases: 
                                /// - is Literal
                                /// - is Value type
                                if (_paramToken.type == "literal")
                                {
                                    /// search for the literal type:
                                    if (_parameterDict.ContainsKey(_paramToken.value.ToString()))
                                    {
                                        /// if it's defined then we will find it there.
                                        _print("\nGot parameter type from {0}.", _paramToken);
                                        isMatched = true;
                                    }
                                    else
                                    {
                                        /// Undefined !
                                        throw new NotImplementedException();
                                    }
                                }
                                else /// Value Type:
                                {
                                    isMatched = _paramToken.type == methodParams[i].ParameterType.Name;
                                    _print("\nGot value type there: {0} | Matched: {1}", _paramToken, isMatched);
                                }
                            }
                            /// BLock ///
                            else if (parameters[i] is Block)
                            {
                                var name = _findBlockReturnType(parameters[i] as Block);
                                if (name != null && name.type == "class")
                                {
                                    /// Ok, here we come to where we expected: 
                                    isMatched = name.value == methodParams[i].ParameterType.Name || methodParams[i].ParameterType.Name == "Object";
                                    _print("\nGot value type there: {0} | Matched: {1}", name, isMatched);
                                }
                            }
                        }
                        if (isMatched) _methodCandidates.Add(methodInfo);
                    }
                }
            }

            return _methodCandidates;
        }

        private static HashList<string> _findParameterTypeOfMethod(string functionFullName, object[] tree, int parameterPosition)
        {
            var _candidates = _findFullNameClasses(functionFullName);
            /// Get possible types from method's parameters:
            var _typeCandidates = _findPossibleParameterTypes(functionFullName, _candidates, tree, parameterPosition);
            return _typeCandidates;
        }

        private class PInfo
        {
            public bool isFunction = false;
            public bool isVerified = false;
            public bool isSameAsReturnType = false;
            public string exactType;
            public string equalTypeNeighbour;
            public PInfo()
            {
            }
        }

        private static Dictionary<string, PInfo> _parameterDict = new Dictionary<string, PInfo>();

        private static string _searchFor(string signature, Block _body, string _functionName)
        {
            var _tree = _body.elements.ToArray();
            for (int i = 0; i < _tree.Length; i++)
            {
                if (_tree[i] is Token)
                {
                    var token = _tree[i] as Token;
                    Console.WriteLine("scanning: '{0}' -> '{1}'", token.value, signature);
                    
                    #region Function -> Return Case 
                    if (token.value == "return" && signature == _functionName)
                        {
                            /// should figure out the type that is returned.
                            if (_tree.Length == 2)
                            {
                                if (_tree[1] is Block)
                                {
                                    /// continue searching for return type !
                                    var _returnType = _findBlockReturnType(_tree[1] as Block);
                                    if (_returnType.type == "literal")
                                    {
                                        _parameterDict[signature].equalTypeNeighbour = _returnType.value;
                                    }
                                    else if (_returnType.type == "class")
                                    {
                                        _parameterDict[signature].isVerified = true;
                                        _parameterDict[signature].exactType = _returnType.value;
                                        /// Done ! We found it !
                                        return _parameterDict[signature].exactType;
                                    }
                                }
                                else if (_tree[1] is Token)
                                {
                                    /// ??? depend on it !
                                    var _atom = _buildAtom(_tree[1] as Token);
                                    if (_atom.type == "literal")
                                    {
                                        _parameterDict[signature].equalTypeNeighbour = (_tree[1] as Token).value;
                                    }
                                    else
                                    {
                                        return _atom.type;
                                    }
                                }
                            }           
                            throw new NotImplementedException();
                    }
                    #endregion

                    #region Identify Host Function that using this variable :
                    else if (token.value == signature)
                    {
                        Console.WriteLine("'{0}' is used by '{1}'.", signature, (_tree[0] as Token).value);
                        /*******************************************
                         *                                         *
                         *  Start identify the host-function !     *
                         *                                         *
                         *******************************************/ 
                        var _f = (_tree[0] as Token).value;
                        if (_f.Contains("."))
                        {
                            var result = _findParameterTypeOfMethod(_f, _tree, i);
                            if (result.Count > 1 || result.Count == 0)
                            {
                                /// Linking collected types to see if they're the same:
                                ///TODO: We should use Dict or Set in this case anyway.
                                var _filter = new HashList<string>();
                                foreach (var candidate in result)
                                    if (!_filter.Contains(candidate))
                                        _filter.Add(candidate);

                                _print("can't determine type using !");
                                throw new Exception();
                            }
                            else return result[0];
                        }
                        else
                        {
                            /// Our Defined Function !
                            var result = _findTypeInLocalFunction(_f, _tree, i, signature, _functionName);
                            if (result == null)
                            {
                                /// it's nothing here.                
                                return null;
                            }
                            else if (result.type == "literal")
                            {
                                /// if dependent variable is there.
                                if (_parameterDict.ContainsKey(result.value) &&
                                    _parameterDict[result.value].isVerified)
                                {
                                    return _parameterDict[result.value].exactType;
                                }
                                else
                                    /// mean it depend on other variable !
                                    _parameterDict[signature].equalTypeNeighbour = result.value;
                                return null;
                            }
                            else return result.type;
                        }
                    }
                    #endregion
                             
                }
                          
                else if (_tree[i] is Block)
                {
                    var result = _searchFor(signature, _tree[i] as Block, _functionName);
                    if (result != null)
                        return result;
                }      
            }
            return null;
            //throw new NotImplementedException();
        }

        private static Token _findBlockReturnType(Block block)
        {
            var f = block.head;
            if (f is Token)
            {
                var fname = (f as Token).value.ToString();
                if (fname.Contains("."))
                {
                    /// .net interop: 
                    var _methods = _findMethodOverload(fname, block.rest);
                    if (_methods.Count > 1)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        var _returnType = _methods[0].ReturnType;
                        return new Token("class", _returnType.Name);
                    }
                }
                else /// Primitives or Local Function:
                {
                    if (_primitiveForms.ContainsKey(fname))
                    {
                        var typeList = new List<string>();
                        /// checking parameters first:
                        foreach (var p in block.rest)
                        {
                            if (p is Token)
                            {
                                var _pToken = p as Token;
                                if (_pToken.type == "literal")
                                {
                                    if (_parameterDict.ContainsKey(_pToken.value))
                                    {
                                        if (_parameterDict[_pToken.value].isVerified)
                                            typeList.Add(_parameterDict[_pToken.value].exactType);
                                        else throw new NotImplementedException();
                                    }
                                }
                                else typeList.Add((p as Token).type);
                            }
                            else if (p is Block)
                            {
                                var ret = _findBlockReturnType(p as Block);
                                typeList.Add(ret.value);
                            }
                        }

                        /// then see which apply the function is:
                        var _primitive = (Expression)Activator.CreateInstance(
                           _primitiveForms[fname],
                           new[] { new object[] { } });

                        if (_primitive.abstractType == ApplyCase.Map)
                        {
                            var _h = new HashList<string>();
                            foreach (var t in typeList) _h.AddUnique(t);
                            if (_h.Count == 1 && _h[0] != "literal") return new Token("class", _h[0]);
                            else throw new NotImplementedException();
                        }

                        else throw new NotImplementedException();
                    }
                    /// local function calling.    
                    /********************************************************************
                     *                                                                  *
                     * simply make a collection of defined function in hosting context. *
                     *                                                                  *
                     ********************************************************************/
                    if (_definedForms.ContainsKey(fname))
                    {
                        return new Token("class", _definedForms[fname].returnType);
                    }
                    throw new NotImplementedException();
                }
            }
            throw new NotImplementedException();
        }

        private static Token _findTypeInLocalFunction
            (string _inspectingMethod, object[] _tree, int _index, string parameterName, string _functionName)
        {
            /// Local Context:
            if (_primitiveForms.ContainsKey(_inspectingMethod))
            {
                var _abstract = ((Expression)Activator.CreateInstance(
                    _primitiveForms[_inspectingMethod], new[] { new object[] { } })).abstractType;
                switch (_abstract)
                {
                    case ApplyCase.Map:
                        /// as pure numeric:
                        var neighbourCandidates = new HashList<Token>();
                        var l = new List<object>(_tree);
                        l.Remove(l[0]);
                        l.Remove(_tree[_index]);
                        if (l.Count > 0)
                        {
                            foreach (var neighbour in l)
                            {
                                if (neighbour is Token)
                                {
                                    var _neighbour = neighbour as Token;
                                    _print("found neighbour {0}:{1}.\n", _neighbour.type, _neighbour.value);
                                    /// Should be primitive type:
                                    neighbourCandidates.AddUnique(_neighbour);
                                }
                                else
                                {
                                    var result = _searchFor(parameterName, neighbour as Block, _functionName);
                                    if (result != null)
                                        neighbourCandidates.AddUnique(new Token(result, parameterName));
                                }
                            }
                            _print("\nFound {0} solution.\n", neighbourCandidates.Count);
                            return neighbourCandidates[0];
                        }
                        break;
                    case ApplyCase.Distinct:
                        /// Where function is not a mapping !
                        /// it mean be specific in the function signature.
                        throw new NotImplementedException();

                    case ApplyCase.Return:
                        _print("\nscanning at return -> {0}", parameterName);
                        _parameterDict[parameterName].isSameAsReturnType = true;

                        break;

                    default: throw new NotImplementedException();
                }
            }
            else
            {
                /// search on defined functions ?
                if (_definedForms.ContainsKey(_inspectingMethod))
                {
                    _print("\nFound defined function: {0}", _inspectingMethod);
                    if (_definedForms[_inspectingMethod].inferenceMap.ContainsKey(parameterName))
                    {
                        var _found = _definedForms[_inspectingMethod].inferenceMap[parameterName];
                        _print("\nFound inferenced type: {0}", _found.value);
                        return _found.InferencedToken;
                    }
                }
                return null;
            }
            return null;                                
        }

        #endregion

    }
}
