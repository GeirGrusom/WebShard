using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WebShard.Ioc;

namespace WebShard.Serialization.Json
{
    static class JsonObjectDeserializer<T>
    {
        private static readonly DeserializeElement<T> deserProc; 

        static JsonObjectDeserializer()
        {
            var ctors = typeof(T).GetConstructors();
            var ctor = ctors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if (ctor == null)
                throw new TypeConstructionException(typeof(T), "The type does not have any public properties.");
            if (ctor.GetParameters().Length == 0)
            {
                // Do nothing for now.
                deserProc = DeserializePropertySets;
            }
            else
            {
                deserProc = CreateConstructorInjector(ctor);
            }
        }

        // TODO: Write compiled version
        private static T DeserializePropertySets(ref IEnumerator<Token> tokenStream)
        {
            var result = Activator.CreateInstance<T>();
            Dictionary<string, PropertyInfo> properties =
                typeof (T).GetProperties()
                    .Where(p => p.CanWrite)
                    .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

            while (tokenStream.Current.Type != TokenType.RightBrace)
            {
                var nameToken = tokenStream.Current;
                var left = JsonStringDeserializer.Deserialize(ref tokenStream);
                PropertyInfo par;
                if (!properties.TryGetValue(left, out par))
                    throw new JsonDeserializationException(nameToken, "Missing field");
                if (tokenStream.Current.Type != TokenType.Colon)
                    throw new JsonDeserializationException(tokenStream.Current, "Expected ':'");
                tokenStream.MoveNext();

                var right = JsonDeserializer.Deserialize(ref tokenStream, par.PropertyType);
                par.SetValue(result, right);

                if (tokenStream.Current.Type != TokenType.Comma)
                    break;
                tokenStream.MoveNext();
            }

            if (tokenStream.Current.Type != TokenType.RightBrace)
                throw new JsonDeserializationException(tokenStream.Current, "Expected '}'");

            return result;
        }

        private static Expression CompareToToken(Expression left, Expression right)
        {
            var compareMethod = typeof (string).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public, null,
                new[] {typeof (string), typeof (string), typeof (StringComparison)}, null);
            return
                Expression.Call(compareMethod, left, right, Expression.Constant(StringComparison.OrdinalIgnoreCase));
        }

        private static Expression CreateSkip(ParameterExpression tokenStream)
        {
            var method = typeof (JsonObjectDeserializer<T>).GetMethod("Skip",
                BindingFlags.Static | BindingFlags.NonPublic);

            return Expression.Call(method, tokenStream);
        }

        private static Expression CreateIfThen(ParameterExpression tokenStream, ParameterExpression variable, Expression variableSet, Expression name, Expression doParse, Expression elseIf)
        {
            var assignParse = Expression.Assign(variable, doParse);
            var assignTrue = Expression.Assign(variableSet, Expression.Constant(true, typeof (bool)));
            if (elseIf != null)
                return Expression.IfThenElse(CompareToToken(name, Expression.Constant(variable.Name)),
                    Expression.Block(assignParse, assignTrue), elseIf);

            return Expression.IfThenElse(CompareToToken(name, Expression.Constant(variable.Name)),
                Expression.Block(assignParse, assignTrue), CreateSkip(tokenStream));
        }

        private static Expression CreateParseExpression(Type type, ParameterExpression tokenStream)
        {
            var dt = JsonDeserializer.GetDeserializer(type);
            var desProc = dt.GetMethod("Deserialize", new[] {typeof (IEnumerator<Token>).MakeByRefType()});
            return Expression.Call(desProc, tokenStream);
        }

        private static Expression CreateIfThenChain(IList<ParameterExpression> input, Expression variableSet, ParameterExpression tokenStream, Expression nameVariable)
        {
            var expression = CreateIfThen(tokenStream, input[input.Count - 1], Expression.ArrayAccess(variableSet, Expression.Constant(input.Count - 1, typeof(int))), nameVariable, CreateParseExpression(input[input.Count - 1].Type, tokenStream), null);
            for (int i = input.Count - 2; i >= 0; i--)
            {
                expression = CreateIfThen(tokenStream, input[i], Expression.ArrayAccess(variableSet, Expression.Constant(i, typeof(int))), nameVariable, CreateParseExpression(nameVariable.Type, tokenStream),
                    expression);
            }
            return expression;
        }

        private static Expression IfNotAssignedSetDefault(ParameterExpression parameter, string fieldName, Expression isNotSet, Expression defaultValue)
        {
            return Expression.IfThen(isNotSet, Expression.Assign(parameter, defaultValue));
        }

        private static Expression AssignDefaultIfNotSet(IList<ParameterExpression> input, Expression variableSet, IList<ParameterInfo> parameters, Expression startToken)
        {
            var results = new Expression[input.Count];
            for (int i = 0; i < input.Count; i++)
            {
                results[i] = IfNotAssignedSetDefault(input[i], parameters[i].Name,
                    Expression.Not(Expression.ArrayIndex(variableSet, Expression.Constant(i))),
                    parameters[i].HasDefaultValue ? Expression.Constant(parameters[i].DefaultValue) : (Expression)Expression.Default(parameters[i].ParameterType));
            }
            return Expression.Block(results);
        }

        private static void Skip(ref IEnumerator<Token> tokenStream)
        {
            if (tokenStream.Current.Type == TokenType.LeftBracket)
            {
                int count = 1;
                while (count > 0 && tokenStream.MoveNext())
                {
                    if (tokenStream.Current.Type == TokenType.LeftBracket)
                        count++;
                    else if (tokenStream.Current.Type == TokenType.RightBracket)
                        count--;
                }
                tokenStream.MoveNext();
            }
            else if (tokenStream.Current.Type == TokenType.LeftBrace)
            {
                int count = 1;
                while (count > 0 && tokenStream.MoveNext())
                {
                    if (tokenStream.Current.Type == TokenType.LeftBrace)
                        count++;
                    else if (tokenStream.Current.Type == TokenType.RightBrace)
                        count--;
                }
                tokenStream.MoveNext();
            }
            else
                tokenStream.MoveNext();
        }

        private static DeserializeElement<T> CreateConstructorInjector(ConstructorInfo ctor)
        {
            var pars = ctor.GetParameters().OrderBy(p => p.Position).ToArray();
            var variablesSet = Expression.Variable(typeof (bool[]), "$variableSet");
            var variables = pars.Select(x => Expression.Variable(x.ParameterType, x.Name)).ToArray();
            var startToken = Expression.Variable(typeof (Token), "$startToken");
            var memberName = Expression.Variable(typeof (string), "$memberName");
            var nameToken = Expression.Variable(typeof (Token), "$memberNameToken");
            var input = Expression.Parameter(typeof (IEnumerator<Token>).MakeByRefType(), "$input");
            var jsonDeserExc = typeof (JsonDeserializationException).GetConstructor(new[] {typeof (Token), typeof(string)});
            var moveNextMethod = typeof (IEnumerator).GetMethod("MoveNext");
            var loop = Expression.Label(typeof(void), "Loop");
            var currentTokenType = Expression.Property(Expression.Property(input, "Current"), "Type");
            var currentToken = Expression.Property(input, "Current");
            var variableCheckChain = CreateIfThenChain(variables, variablesSet, input, memberName);

            if(jsonDeserExc == null)
                throw new MissingMethodException("JsonDeserializationException", "ctor(Token, String)");
            // if(input.Current.Type != TokenType.LeftBrace) 
            //    throw new JsonDeserializationException($input.Current);
            
            var throwIfNotLeftBrace = Expression.IfThen(
                Expression.NotEqual(currentTokenType,
                    Expression.Constant(TokenType.LeftBrace, typeof (TokenType))),
                    Expression.Throw(Expression.New(jsonDeserExc, currentToken, Expression.Constant("Expected '{'."))));

            var throwIfNotColon = Expression.IfThen(
                Expression.NotEqual(currentTokenType,
                    Expression.Constant(TokenType.Colon, typeof(TokenType))),
                    Expression.Throw(Expression.New(jsonDeserExc, currentToken, Expression.Constant("Expected ':'."))));

            var moveNext = Expression.Call(input, moveNextMethod);
            var stringDeserializeMethod = typeof (JsonStringDeserializer).GetMethod("Deserialize",
                BindingFlags.Public | BindingFlags.Static, null, new[] {typeof (IEnumerator<Token>).MakeByRefType()},
                null);
            var getName = Expression.Assign(memberName,
                Expression.Call(stringDeserializeMethod, input));
            var setMemberNameToken = Expression.Assign(nameToken, currentToken);
            var exit = Expression.Label(typeof (T), "exit");
            var emptyObject = Expression.Label(typeof (void), "emptyObject");
            var ifIsRightBraceGotoEmptyObject =
                Expression.IfThen(
                    Expression.Equal(currentTokenType, Expression.Constant(TokenType.RightBrace, typeof (TokenType))),
                    Expression.Goto(emptyObject));
            var continueIfComma =
                Expression.IfThen(
                    Expression.Equal(currentTokenType,
                        Expression.Constant(TokenType.Comma)), Expression.Goto(loop));


            //var throwIncompleteTypeException = Expression.Throw(Expression.New(jsonDeserExc, startToken));
            var assignDefaultValueIfNotSet = AssignDefaultIfNotSet(variables, variablesSet, pars, startToken);

            var throwIfNotRightBrace =
                Expression.IfThen(
                    Expression.NotEqual(currentTokenType, Expression.Constant(TokenType.RightBrace, typeof (TokenType))),
                    Expression.Throw(Expression.New(jsonDeserExc, currentToken, Expression.Constant("Expected '}'"))));

            var lambda = Expression.Lambda<DeserializeElement<T>>(
                Expression.Block(variables.Concat(new[] {memberName, variablesSet, startToken, nameToken}),
                    Expression.Assign(startToken, Expression.Property(input, "Current")),
                    throwIfNotLeftBrace,
                    Expression.Assign(variablesSet,
                        Expression.NewArrayBounds(typeof (bool),
                            new Expression[] {Expression.Constant(ctor.GetParameters().Length)})),

                    Expression.Label(loop), 
                    moveNext,
                    ifIsRightBraceGotoEmptyObject,
                    setMemberNameToken,
                    getName,
                    throwIfNotColon,
                    moveNext,
                    variableCheckChain,
                    continueIfComma,
                    Expression.Label(emptyObject),
                    throwIfNotRightBrace,
                    assignDefaultValueIfNotSet,
                    Expression.Label(exit, Expression.New(ctor, variables.Cast<Expression>()))
                    ), input);

            return lambda.Compile();
        }

        // TODO: Write a compiled version.
        public static T Deserialize(ref IEnumerator<Token> tokenStream)
        {
            var token = tokenStream.Current;
            if (token.Type == TokenType.Null)
            {
                tokenStream.MoveNext();
                return default(T);
            }

            return deserProc(ref tokenStream);

        }
    }

    static class JsonObjectDeserializer
    {
        public static object Deserialize(ref IEnumerator<Token> tokenStream)
        {
            var token = tokenStream.Current;
            if (token.Type == TokenType.LeftBrace)
                return JsonDictionaryDeserializer<ExpandoObject, string, object>.Deserialize(ref tokenStream);
            if (token.Type == TokenType.String || token.Type == TokenType.Identifier)
                return JsonStringDeserializer.Deserialize(ref tokenStream);
            if (token.Type == TokenType.LeftBracket)
                return JsonArrayDeserializer<object>.Deserialize(ref tokenStream);
            if (token.Type == TokenType.Null)
            {
                tokenStream.MoveNext();
                return null;
            }
            if (token.Type == TokenType.Number)
                return JsonParseNumberStylesFormatProviderDeserializer<decimal>.Deserialize(ref tokenStream);
            if (token.Type == TokenType.False || token.Type == TokenType.True)
                return JsonBoolDeserializer.Deserialize(ref tokenStream);
            
            throw new JsonDeserializationException(token, string.Format("Unexpected '{0}' at this point.", token.Value));
        }
    }
}