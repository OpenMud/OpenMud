using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DefaultEcs;
using DefaultEcs.System;
using OpenMud.Mudpiler.Core.Components;

namespace OpenMud.Mudpiler.Core.Systems;

[With(typeof(ParseCommandComponent))]
public class CommandParserSystem : AEntitySetSystem<float>
{
    public CommandParserSystem(World world, bool useBuffer = false) : base(world, useBuffer)
    {

    }

    private string[] ExtractOperands(string commandString)
    {
        var operands = new List<string>();
        var isString = false;
        var isEscaped = false;

        string currentOperand = "";

        void Next()
        {
            if(currentOperand.Length > 0)
                operands.Add(currentOperand);

            currentOperand = "";
        }

        foreach (var c in commandString)
        {
            if (isEscaped)
            {
                currentOperand += c;
                isEscaped = false;
            } else if (isString)
            {
                if (c == '\\' && !isEscaped)
                    isEscaped = true;
                else
                {
                    currentOperand += c;

                    if (c == '"' && !isEscaped)
                    {
                        isString = false;
                        currentOperand = currentOperand.Substring(1, currentOperand.Length - 2);
                        Next();
                    }

                    isEscaped = false;
                }
            }
            else
            {

                switch (c)
                {
                    case ' ':
                        Next();
                        break;
                    case '"':
                        isString = true;
                        currentOperand += c;
                        break;
                    default:
                        currentOperand += c;
                        break;
                }
            }
        }

        Next();

        return operands.ToArray();
    }
    
    protected override void Update(float state, in Entity entity)
    {
        var parse = entity.Get<ParseCommandComponent>();

        entity.Remove<ParseCommandComponent>();

        var operands = ExtractOperands(parse.Command);

        entity.Set(new ExecuteCommandComponent(parse.Source, parse.Target, operands.First(), operands.Skip(1).ToArray()));
    }
}
