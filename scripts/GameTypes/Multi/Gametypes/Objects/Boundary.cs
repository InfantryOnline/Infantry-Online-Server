using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public class Boundary
    {
        public short _top;
        public short _bottom;
        public short _left;
        public short _right;

        public short _nextTop;
        public short _nextBottom;
        public short _nextLeft;
        public short _nextRight;

        public short _previousTop;
        public short _previousBottom;
        public short _previousLeft;
        public short _previousRight;


        private int tickLastWave;
        private Arena _arena;
        private int _tickLastShrink;

        private bool _bShrink;
        private bool _bShrinkVertical;
        private bool _bNextShrinkVertical;
        private int _shrinkAmount;
        private int _lastShrink;

        private int _tickStormStart;
        private int _lastStormDamage;
        private ItemInfo.RepairItem oobEffect = AssetManager.Manager.getItemByID(172) as ItemInfo.RepairItem;

        private bool _bBoundariesDrawn;

        public short Height
        {
            get
            {
                return (short)(_bottom - _top);
            }
        }

        public short Width
        {
            get
            {
                return (short)(_right - _left);
            }
        }

        public short nextHeight
        {
            get
            {
                return (short)(_nextBottom - _nextTop);
            }
        }

        public short nextWidth
        {
            get
            {
                return (short)(_nextRight - _nextLeft);
            }
        }

        public Boundary(Arena arena, short top, short bottom, short right, short left)
        {
            _arena = arena;
            _top = top;
            _bottom = bottom;
            _left = left;
            _right = right;
            _nextTop = top;
            _nextBottom = bottom;
            _nextLeft = left;
            _nextRight = right;

            _bShrink = false;
            _bBoundariesDrawn = false;
            _lastStormDamage = Environment.TickCount; 
            _tickLastShrink = Environment.TickCount;
            _tickStormStart = Environment.TickCount;

        }

        public void Shrink(short shrinkAmount, bool bShrinkVertical)
        {
            _shrinkAmount = shrinkAmount;
            _bShrinkVertical = bShrinkVertical;
            _bShrink = true;
            _bBoundariesDrawn = false;
            _arena.sendArenaMessage("Play area is shrinking, Don't get caught in the storm!");
            _arena.setTicker(1, 3, 0, "Storm is approaching fast! Seek Cover!");
        }

        public void calculateNextShrinkBoundary()
        {
            short shrinkAmount = 0;
            _bNextShrinkVertical = false;

            if (nextWidth < 1500)
            {
                _bNextShrinkVertical = true;
                shrinkAmount = (short)((nextHeight) * 0.50);
            }

            else
            {
                _bNextShrinkVertical = false;
                shrinkAmount = (short)((nextWidth) * 0.50);
            }
            
            if (_bNextShrinkVertical)
            {
                double realShrinkAmountMultiplier = Math.Ceiling(((double)shrinkAmount / 80));
                double realShrinkAmount = 80 * realShrinkAmountMultiplier;

                _nextTop += (short)((realShrinkAmount / 2));
                _nextBottom -= (short)((realShrinkAmount / 2));
                _nextLeft = _left;
                _nextRight = _right;

            }
            else
            {
                double realShrinkAmountMultiplier = Math.Ceiling(((double)shrinkAmount / 80));
                double realShrinkAmount = 80 * realShrinkAmountMultiplier;

                _nextTop = _top;
                _nextBottom = _bottom;
                _nextLeft += (short)(realShrinkAmount / 2);
                _nextRight -= (short)(realShrinkAmount / 2);

            }

        }

        
        public void drawCurrentBoundary()
        {
            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();
            short _middle = (short)(_top + (_bottom - _top) / 2);
            short _middleTop = (short)(_top + (_middle - _top) / 2);
            short _middleBottom = (short)(_middle + (_bottom - _middle) / 2);

            state.positionX = _right;
            state.positionY = _bottom;
            target.positionX = _right;
            target.positionY = _top;

            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);
            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000); // Right, Bottom to Top
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _right, _bottom, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _right, _middle, 0, fireAngle, 0); //Middle
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _right, _middleTop, 0, fireAngle, 0); //MiddleTop
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _right, _middleBottom, 0, fireAngle, 0); //MiddleBottom
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _right, _bottom, 0, fireAngle, 0);//vis


            state.positionX = _right;
            state.positionY = _top;
            target.positionX = _right;
            target.positionY = _bottom;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);  //Right, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _right, _top, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _right, _middle, 0, fireAngle, 0); // Middle
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _right, _middleTop, 0, fireAngle, 0); // MiddleTop
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _right, _middleBottom, 0, fireAngle, 0); // MiddleBottom

            state.positionX = _left;
            state.positionY = _top;
            target.positionX = _left;
            target.positionY = _bottom;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000); //Left, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _left, _top, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _left, _middle, 0, fireAngle, 0); // Middle
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _left, _middleTop, 0, fireAngle, 0); // MiddleTop
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _left, _middleBottom, 0, fireAngle, 0); // MiddleBottom
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _left, _top, 0, fireAngle, 0); // vis


            state.positionX = _left;
            state.positionY = _bottom;
            target.positionX = _left;
            target.positionY = _top;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);  //Left, Bottom to Top
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _left, _bottom, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _left, _middle, 0, fireAngle, 0); //From Middle
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _left, _middleTop, 0, fireAngle, 0); //From MiddleTop
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _left, _middleBottom, 0, fireAngle, 0); //From MiddleBottom
        }

        public void drawNextBoundary()
        {
            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();
            short _nextMiddle = (short)(_nextTop + (_nextBottom - _nextTop) / 2);
            short _nextMiddleTop = (short)(_nextTop + (_nextMiddle - _nextTop) / 2);
            short _nextMiddleBottom = (short)(_nextMiddle + (_nextBottom - _nextMiddle) / 2);

            state.positionX = _nextRight;
            state.positionY = _nextBottom;
            target.positionX = _nextRight;
            target.positionY = _nextTop;

            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);
            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000); // Right, Bottom to Top
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextRight, _nextBottom, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextRight, _nextMiddle, 0, fireAngle, 0); //Middle
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextRight, _nextMiddleTop, 0, fireAngle, 0); //MiddleTop
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextRight, _nextMiddleBottom, 0, fireAngle, 0); //MiddleBottom
            Helpers.Player_RouteExplosion(_arena.Players, 1468, _nextRight, _nextBottom, 0, fireAngle, 0);//vis


            state.positionX = _nextRight;
            state.positionY = _nextTop;
            target.positionX = _nextRight;
            target.positionY = _nextBottom;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);  //Right, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextRight, _nextTop, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextRight, _nextMiddle, 0, fireAngle, 0); // Middle
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextRight, _nextMiddleTop, 0, fireAngle, 0); // MiddleTop
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextRight, _nextMiddleBottom, 0, fireAngle, 0); // MiddleBottom

            state.positionX = _nextLeft;
            state.positionY = _nextTop;
            target.positionX = _nextLeft;
            target.positionY = _nextBottom;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000); //Left, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextLeft, _nextTop, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextLeft, _nextMiddle, 0, fireAngle, 0); // Middle
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextLeft, _nextMiddleTop, 0, fireAngle, 0); // MiddleTop
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextLeft, _nextMiddleBottom, 0, fireAngle, 0); // MiddleBottom
            Helpers.Player_RouteExplosion(_arena.Players, 1468, _nextLeft, _nextTop, 0, fireAngle, 0); // vis


            state.positionX = _nextLeft;
            state.positionY = _nextBottom;
            target.positionX = _nextLeft;
            target.positionY = _nextTop;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);  //Left, Bottom to Top
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextLeft, _nextBottom, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextLeft, _nextMiddle, 0, fireAngle, 0); //From Middle
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextLeft, _nextMiddleTop, 0, fireAngle, 0); //From MiddleTop
            Helpers.Player_RouteExplosion(_arena.Players, 1463, _nextLeft, _nextMiddleBottom, 0, fireAngle, 0); //From MiddleBottom
        }

        public void drawCurrentRectangleBoundary()
        {
            short circleMarkLocation = _left;
            short distanceBetweenCircleMarks = 100;

            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();
            state.positionX = _right;
            state.positionY = _top;
            target.positionX = _right;
            target.positionY = _bottom;
            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);

            while (circleMarkLocation < _right)
            {
                Helpers.Player_RouteExplosion(_arena.Players, 1470, circleMarkLocation, _top, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 1470, circleMarkLocation, _bottom, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }
            circleMarkLocation = _top;
            while (circleMarkLocation < _bottom)
            {
                Helpers.Player_RouteExplosion(_arena.Players, 1470, _left, circleMarkLocation, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 1470, _right, circleMarkLocation, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }

        }

        public void drawNextRectangleBoundary()
        {
            short circleMarkLocation = _nextLeft;
            short distanceBetweenCircleMarks = 100;

            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();
            state.positionX = _right;
            state.positionY = _top;
            target.positionX = _right;
            target.positionY = _bottom;
            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);

            while (circleMarkLocation < _nextRight)
            {
                Helpers.Player_RouteExplosion(_arena.Players, 1471, circleMarkLocation, _nextTop, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 1471, circleMarkLocation, _nextBottom, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }

            if (Width != nextWidth)
            {
                circleMarkLocation = _nextTop;
                while (circleMarkLocation < _nextBottom)
                {
                    Helpers.Player_RouteExplosion(_arena.Players, 1471, _nextLeft, circleMarkLocation, 0, fireAngle, 0);
                    Helpers.Player_RouteExplosion(_arena.Players, 1471, _nextRight, circleMarkLocation, 0, fireAngle, 0);
                    circleMarkLocation += distanceBetweenCircleMarks;
                }
            }
            

        }

        public void drawHorizontalBoundaryShrink(int now)
        {
            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();

            state.positionX = _right;
            state.positionY = _bottom;
            target.positionX = _right;
            target.positionY = _top;

            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);
            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000); // Right, Bottom to Top
            Helpers.Player_RouteExplosion(_arena.Players, 1472, _right, _bottom, 0, fireAngle, 0);//vis


                Helpers.Player_RouteExplosion(_arena.Players, 1472, _nextRight, _nextBottom, 0, fireAngle, 0);//vis where it ends

            state.positionX = _left;
            state.positionY = _top;
            target.positionX = _left;
            target.positionY = _bottom;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000); //Left, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 1472, _left, _top, 0, fireAngle, 0); // vis


            Helpers.Player_RouteExplosion(_arena.Players, 1472, _nextLeft, _nextTop, 0, fireAngle, 0); // vis where it ends

        }
        public void drawVerticalBoundaryShrink(int now)
        {
            short circleMarkLocation = _left;
            short distanceBetweenCircleMarks = 100;

            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();
            state.positionX = _right;
            state.positionY = _top;
            target.positionX = _right;
            target.positionY = _bottom;
            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);

            while (circleMarkLocation < _right)
            {
                Helpers.Player_RouteExplosion(_arena.Players, 1473, circleMarkLocation, _top, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 1473, circleMarkLocation, _bottom, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }


                circleMarkLocation = _previousTop;
                while (circleMarkLocation < _previousBottom)
                {
                    Helpers.Player_RouteExplosion(_arena.Players, 1473, _left, circleMarkLocation, 0, fireAngle, 0);
                    Helpers.Player_RouteExplosion(_arena.Players, 1473, _right, circleMarkLocation, 0, fireAngle, 0);
                    circleMarkLocation += distanceBetweenCircleMarks;
                }




        }

        public void Poll(int now)
        {

            if (now - _tickLastShrink >= 60 * 1000)
            {
                if (!_bShrink)
                {
                    _previousTop = _top;
                    _previousBottom = _bottom;
                    _previousLeft = _left;
                    _previousRight = _right;

                    short shrinkAmount = (short)((Width + Height) * 0.50);
                    if (Width < 1500) 
                    {
                        shrinkAmount = (short)((Height) * 0.50);
                        Shrink(shrinkAmount, true);
                    }

                    else
                    {
                        shrinkAmount = (short)((Width) * 0.50);
                        Shrink(shrinkAmount, false);
                    }
                }
            }

            if (!_bShrink && !_bBoundariesDrawn)
            {
                calculateNextShrinkBoundary();
                if (nextHeight < 3000)
                {
                    drawNextRectangleBoundary();
                    if (Height < 3000)
                    {
                        drawCurrentRectangleBoundary();
                    }
                    else
                    {
                        drawCurrentBoundary();
                    }
                }
                else
                {
                    drawCurrentBoundary();
                    drawNextBoundary();
                }

                _bBoundariesDrawn = true;
            }

            if (_bShrink && now - _lastShrink >= 250)
            {
                if (_shrinkAmount <= 0)
                {
                    _bShrink = false;
                    _bShrinkVertical = false;
                    _shrinkAmount = 0;
                    _tickLastShrink = now;
                    _arena.setTicker(1, 3, 60 * 100, "Play area shrinking in: ");
                }

                if (_bShrinkVertical)
                {
                    _top += 40;
                    _bottom -= 40;

                    _shrinkAmount -= 80; //used to be 80
                }
                else if (_bShrink)
                {
                    _left += 40;
                    _right -= 40;

                    _shrinkAmount -= 80;
                }

                _lastShrink = now;
            }
            
            if ((now - tickLastWave >= 250) && _bShrink)
            {
                if (Width != nextWidth)
                    drawHorizontalBoundaryShrink(now);
                else
                    drawVerticalBoundaryShrink(now);

                tickLastWave = now;
            }

            if ((now - _lastStormDamage >= 1000) && (now - _tickStormStart >= 5000))
            {
                foreach (Player player in _arena.PlayersIngame)
                {
                    if (player._team._name == "Red" || player._team._name == "Blue")
                        continue;

                    if (!player.inArea(_left, _top, _right, _bottom))
                        player.heal(oobEffect, player);
                }
                _lastStormDamage = now;
            }
        }
    }
}
