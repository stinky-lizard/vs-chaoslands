using Vintagestory.API.Common;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace ChaosLands
{
    public class EntityBehaviorBossWaterRemoval : EntityBehavior
    {
        float timer;

        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);

            timer += deltaTime;

            if (timer >= 10)
            {
                timer = 0;
                
                if (entity.FeetInLiquid)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        FloodFillAt(entity.SidedPos.AsBlockPos.X, entity.SidedPos.AsBlockPos.Y + i, entity.SidedPos.AsBlockPos.Z);
                    }
                }
            }

        }

        public void FloodFillAt(int posX, int posY, int posZ)
        {
            Queue<Vec4i> bfsQueue = new Queue<Vec4i>();
            HashSet<BlockPos> fillablePositions = new HashSet<BlockPos>();

            bfsQueue.Enqueue(new Vec4i(posX, posY, posZ, 0));
            fillablePositions.Add(new BlockPos(posX, posY, posZ));

            float radius = 48;

            BlockFacing[] faces = BlockFacing.HORIZONTALS;
            BlockPos curPos = new BlockPos();

            while (bfsQueue.Count > 0)
            {
                Vec4i bpos = bfsQueue.Dequeue();

                foreach (BlockFacing facing in faces)
                {
                    curPos.Set(bpos.X + facing.Normali.X, bpos.Y + facing.Normali.Y, bpos.Z + facing.Normali.Z);

                    Block block = entity.World.BlockAccessor.GetBlock(curPos);
                    bool inBounds = bpos.W < radius;

                    if (inBounds)
                    {
                        if (block.IsLiquid() && !fillablePositions.Contains(curPos))
                        {
                            bfsQueue.Enqueue(new Vec4i(curPos.X, curPos.Y, curPos.Z, bpos.W + 1));
                            fillablePositions.Add(curPos.Copy());
                        }

                    }
                }
            }

            foreach (BlockPos p in fillablePositions)
            {
                entity.World.BlockAccessor.SetBlock(0, p);
            }
        }

        public EntityBehaviorBossWaterRemoval(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "watergone";
        }
    }
}
