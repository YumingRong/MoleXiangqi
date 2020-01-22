using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//凡是以i开头的变量都是为了与图形界面沟通使用的

namespace MoleXiangqi
{
    partial class POSITION
    {
        public int nHalfClockMove;  //120步不吃子作和的自然限招
        public int nRepBegin;       //可能形成重复局面的起始计数步
        int nStep;                  //步数

        enum GameResult {WIN, DRAW, LOSE, ONGOING };

        // 1. 首先检测历史局面中是否有当前局面，如果没有，就用不着判断了
        //public GameResult Repitition()
        //{
        //    int nRepStart;
        //    for (nRepStart = nStep-4; nRepStart>= nRepBegin; nRepStart-=2)
        //    {
        //        if (zobristRecords[nRepStart] == zobristRecords[nStep])
        //            break;
        //    }
        //    if (nRepStart < nRepBegin)
        //        return GameResult.ONGOING;

        //    // 2. 判断长照
        //    bool bPerpCheck = true;
        //    for (int i = nStep - 1; i >= nRepStart; i -= 2)
        //    {
        //        if (!Check)
        //        {
        //            bPerpCheck = false;
        //            break;
        //        }
        //    }

        //}

    }
}
