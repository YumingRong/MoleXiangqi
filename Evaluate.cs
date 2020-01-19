using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XQWizardLight
{
    partial class Position
    {
        //各种子力的价值
        const int MAT_KING = 1000;
        const int MAT_ROOK = 100;
        const int MAT_CANNON = 45;
        const int MAT_KNIGHT = 45;
        const int MAT_PAWN = 10;
        const int MAT_BISHOP = 20;
        const int MAT_ADVISOR = 20;

        static readonly int[] cnPieceValue = { 0, MAT_ROOK, MAT_CANNON, MAT_KNIGHT, MAT_PAWN, MAT_KING, MAT_BISHOP, MAT_ADVISOR };
        // 1. 开中局、有进攻机会的帅(将)和兵(卒)，参照“梦入神蛋”
        static readonly int[] cKingPawnMidgameAttacking = {
  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
  0,  0,  0,  9,  9,  9, 11, 13, 11,  9,  9,  9,  0,  0,  0,  0,
  0,  0,  0, 19, 24, 34, 42, 44, 42, 34, 24, 19,  0,  0,  0,  0,
  0,  0,  0, 19, 24, 32, 37, 37, 37, 32, 24, 19,  0,  0,  0,  0,
  0,  0,  0, 19, 23, 27, 29, 30, 29, 27, 23, 19,  0,  0,  0,  0,
  0,  0,  0, 14, 18, 20, 27, 29, 27, 20, 21, 15,  0,  0,  0,  0,
  0,  0,  0,  7,  0, 13,  0, 16,  0, 13,  0,  7,  0,  0,  0,  0,
  0,  0,  0,  0,  0,  7,  0, 15,  0,  7,  0,  0,  0,  0,  0,  0,
  0,  0,  0,  0,  0,  0,  1,  1,  1,  0,  0,  0,  0,  0,  0,  0,
  0,  0,  0,  0,  0,  0,  2,  2,  2,  0,  0,  0,  0,  0,  0,  0,
  0,  0,  0,  0,  0,  0, 11, 15, 11,  0,  0,  0,  0,  0,  0,  0,
  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0
};


        public int Evaluate()
        {
            int nPiece = 0;
            int[] materialValue = new int[2];
            materialValue[0] = materialValue[1] = 0;
            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                for (int i = bas; i < bas + 16; i++)
                {
                    int pcKind = cnPieceKinds[i];
                    if (pcKind > 0)
                    {
                        materialValue[sd] += cnPieceValue[cnPieceKinds[i]];
                        nPiece++;
                    }
                }
            }

            //棋盘上的子越多，炮的威力越大，马的威力越小
            int[] populationBobus = new int[2];
            populationBobus[0] = populationBobus[1] = 0;
            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                for (int pc = bas + CANNON_FROM; pc <= bas + CANNON_TO; pc++)
                {
                    if (sqPieces[pc] > 0)
                        populationBobus[sd] += 2 * nPiece;  //2是可调系数
                }
                for (int pc = bas + KNIGHT_FROM; pc <= bas + KNIGHT_TO; pc++)
                {
                    if (sqPieces[pc] > 0)
                        populationBobus[sd] -= 2 * nPiece;  //2是可调系数
                }
            }
            return materialValue[0] - materialValue[1] + populationBobus[0] - populationBobus[1];
        }
    }
}
