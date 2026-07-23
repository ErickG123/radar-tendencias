export type NodeType = 'inicio' | 'fim' | 'condicao' | 'acao';

export interface Port {
  id: string;
  type: 'in' | 'out';
}

export interface FlowNode {
  id: string;
  type: NodeType;
  label: string;
  position: { x: number; y: number };
  ports: Port[];
}

export interface FlowConnection {
  id: string;
  sourceNodeId: string;
  sourcePortId: string;
  targetNodeId: string;
  targetPortId: string;
}
