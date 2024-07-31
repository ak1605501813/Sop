using MstSopService.DTO;
using MstSopService.Entity;
using System.Collections.Generic;
using System.Linq;

namespace MstSopService.Tools
{
    public class ToTreeTool
    {
        public static List<FrameDataOut> BulidTreeBySopBaseDTO(List<FrameDataOut> sopBases,List<FrameDataOut> tree,int pid)
        {
            tree = new List<FrameDataOut>();
            List<FrameDataOut> parentLevel = sopBases.Where(c => c.Pid == pid).ToList();
            if (parentLevel.Count() > 0)
            {
                parentLevel= parentLevel.OrderBy(c => c.Orderid).ToList();
                for (int i = 0; i < parentLevel.Count; i++)
                {
                    parentLevel[i].Subsets = BulidTreeBySopBaseDTO(sopBases, tree, parentLevel[i].Idx);
                    tree.Add(parentLevel[i]);
                }
            }
            return tree;
        }
        public static List<ArchitectureDataOut> BulidTreeByFilesShareDTO(List<ArchitectureDataOut> sopBases, List<ArchitectureDataOut> tree, int pid)
        {
            tree = new List<ArchitectureDataOut>();
            List<ArchitectureDataOut> parentLevel = sopBases.Where(c => c.Pid == pid).ToList();
            if (parentLevel.Count() > 0)
            {
                parentLevel=parentLevel.OrderBy(x => x.Type).ToList();
                for (int i = 0; i < parentLevel.Count; i++)
                {
                    parentLevel[i].Subsets = BulidTreeByFilesShareDTO(sopBases, tree, parentLevel[i].Id);
                    tree.Add(parentLevel[i]);
                }
            }
            return tree;
        }
        public static List<SopOrderAttributeDTO> BulidTreeByOrderAttributeDTO(List<SopOrderAttributeDTO> sopBases, List<SopOrderAttributeDTO> tree, List<Dictinfo> dictinfos, int pid)
        {
            tree = new List<SopOrderAttributeDTO>();
            List<SopOrderAttributeDTO> parentLevel = sopBases.Where(c => c.Pid == pid).ToList();
            if (parentLevel.Count() > 0)
            {
                for (int i = 0; i < parentLevel.Count; i++)
                {
                    parentLevel[i].Subsets = BulidTreeByOrderAttributeDTO(sopBases, tree, dictinfos, parentLevel[i].Id);

                    if (!string.IsNullOrEmpty(parentLevel[i].Dictid)) 
                    {
                        var dics = dictinfos.Where(x => x.Dictid == parentLevel[i].Dictid).ToList();
                        if (dics.Count() > 0) 
                        {
                            foreach (var dic in dics) 
                            {
                                var orderAttr = new SopOrderAttributeDTO()
                                {
                                    Id = dic.Idx,
                                    AttrName = dic.Cname,
                                    AttrNameen = dic.Ename,
                                };
                                parentLevel[i].Subsets.Add(orderAttr);
                            }
                        }
                    }

                tree.Add(parentLevel[i]);
                }
            }
            return tree;
        }
        
    }
}
