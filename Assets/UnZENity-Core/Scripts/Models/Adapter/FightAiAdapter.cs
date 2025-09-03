using System.Collections.Generic;
using GUZ.Core.Const;
using MyBox;
using ZenKit.Daedalus;

namespace GUZ.Core.Models.Adapter
{
    /// <summary>
    /// Pre-Cache FightAiMoves for better debug capabilities. Otherwise, it needs to be fetched at each AI_Attack() call.
    /// </summary>
    public class FightAiAdapter
    {
        private readonly FightAiInstance _fightAiInstance;

        public readonly int MoveCount;
        public readonly FightAiMove[] Moves;
        public FightAiMove GetRandomMove() => Moves.GetRandom();
        
        public FightAiAdapter(FightAiInstance fightAiInstance)
        {
            _fightAiInstance = fightAiInstance;

            CalculateAttackMoves(out MoveCount, out Moves);
        }

        /// <summary>
        /// Caching of Moves so that we don't need to recalculate them with each usage.
        /// </summary>
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
