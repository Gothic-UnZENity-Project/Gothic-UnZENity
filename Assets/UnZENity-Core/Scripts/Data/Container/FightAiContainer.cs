using System.Collections.Generic;
using GUZ.Core.Globals;
using MyBox;
using ZenKit.Daedalus;

namespace GUZ.Core.Data.Container
{
    /// <summary>
    /// Pre-Cache FightAiMoves for better debug capabilities. Otherwise, it needs to be fetched at each AI_Attack() call.
    /// </summary>
    public class FightAiContainer
    {
        private readonly FightAiInstance _fightAiInstance;

        public readonly int MoveCount;
        public readonly FightAiMove[] Moves;
        public FightAiMove GetRandomMove() => Moves.GetRandom();
        
        public FightAiContainer(FightAiInstance fightAiInstance)
        {
            _fightAiInstance = fightAiInstance;

            CalculateAttackMoves(out MoveCount, out Moves);
        }

        private void CalculateAttackMoves(out int moveCount, out FightAiMove[] moves)
        {
            var tempMoves = new List<FightAiMove>();
            
            for (moveCount = 0; moveCount < FightConst.FightAiMoveMax; moveCount++)
            {
                // Load all move entries in list until the value is 0 aka unset.
                if (_fightAiInstance.GetMove(moveCount) == FightAiMove.Nop)
                    break;
                
                tempMoves.Add(_fightAiInstance.GetMove(moveCount));
            }
            
            moves = tempMoves.ToArray();
        }
    }
}
