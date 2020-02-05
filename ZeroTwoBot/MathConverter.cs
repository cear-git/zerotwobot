using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus;
using DSharpPlus.CommandsNext.Converters;

namespace ZeroTwoBot
{
    class MathConverter : IArgumentConverter<MathOperation>
    {
        public bool TryConvert(string value, CommandContext ctx, out MathOperation result)
        {
            switch (value)
            {
                case "+":
                    result = MathOperation.Add;
                    return true;

                case "-":
                    result = MathOperation.Subtract;
                    return true;

                case "x":
                    result = MathOperation.Multiply;
                    return true;

                case "/":
                    result = MathOperation.Divide;
                    return true;

                case "%":
                    result = MathOperation.Percent;
                    return true;
                    

            }

            result = MathOperation.Add;
            return false;
        }
    }
}
