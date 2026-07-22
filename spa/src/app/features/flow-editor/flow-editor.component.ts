import { Component, signal, ElementRef, ViewChild, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DragDropModule, CdkDragEnd } from '@angular/cdk/drag-drop';
import { FlowNode, FlowConnection, Port } from '../../core/models/flow.model';
import { FlowService } from '../../core/services/flow.service';

@Component({
  selector: 'app-flow-editor',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  templateUrl: './flow-editor.component.html',
  styleUrls: ['./flow-editor.component.scss']
})
export class FlowEditorComponent {
  @ViewChild('canvas', { static: true }) canvas!: ElementRef;
  
  private flowService = inject(FlowService);

  nodes = signal<FlowNode[]>([
    { id: 'node-1', type: 'inicio', label: 'Início', position: { x: 50, y: 150 }, ports: [{ id: 'p1', type: 'out' }] },
    { id: 'node-2', type: 'condicao', label: 'Hype > 80', position: { x: 300, y: 150 }, ports: [{ id: 'p2', type: 'in' }, { id: 'p3', type: 'out' }] },
    { id: 'node-3', type: 'fim', label: 'Disparar Alerta', position: { x: 600, y: 150 }, ports: [{ id: 'p4', type: 'in' }] }
  ]);

  connections = signal<FlowConnection[]>([]);
  drawing = signal<{ isDrawing: boolean, sourceNodeId: string, sourcePortId: string, startX: number, startY: number, endX: number, endY: number } | null>(null);

  salvarFluxo() {
    const payload = {
      nome: 'Regra Dinâmica V1',
      nodes: this.nodes().map(n => ({
        nodeID: n.id,
        tipo: n.type,
        label: n.label,
        posX: n.position.x,
        posY: n.position.y
      })),
      connections: this.connections().map(c => ({
        connectionID: c.id,
        sourceNodeID: c.sourceNodeId,
        sourcePortID: c.sourcePortId,
        targetNodeID: c.targetNodeId,
        targetPortID: c.targetPortId
      }))
    };

    this.flowService.salvarFluxo(payload).subscribe({
      next: () => alert('Fluxo salvo com sucesso no banco de dados!'),
      error: (err) => alert('Erro ao salvar fluxo: ' + err.message)
    });
  }

  carregarFluxo() {
    this.flowService.getFluxos().subscribe(fluxos => {
      if (fluxos && fluxos.length > 0) {
        const fluxo = fluxos[fluxos.length - 1];
        
        const loadedNodes: FlowNode[] = fluxo.nodes.map((n: any) => {
          let ports: Port[] = [];
          if (n.tipo === 'inicio') ports = [{ id: 'p1', type: 'out' }];
          else if (n.tipo === 'condicao') ports = [{ id: 'p1', type: 'in' }, { id: 'p2', type: 'out' }];
          else if (n.tipo === 'acao') ports = [{ id: 'p1', type: 'in' }, { id: 'p2', type: 'out' }];
          else if (n.tipo === 'fim') ports = [{ id: 'p1', type: 'in' }];

          return {
            id: n.nodeID,
            type: n.tipo,
            label: n.label,
            position: { x: n.posX, y: n.posY },
            ports
          };
        });

        const loadedConns: FlowConnection[] = fluxo.connections.map((c: any) => ({
          id: c.connectionID,
          sourceNodeId: c.sourceNodeID,
          sourcePortId: c.sourcePortID,
          targetNodeId: c.targetNodeID,
          targetPortId: c.targetPortID
        }));

        this.nodes.set(loadedNodes);
        this.connections.set(loadedConns);
      } else {
        alert('Nenhum fluxo encontrado no banco.');
      }
    });
  }

  onDragEnded(event: CdkDragEnd, node: FlowNode) {
    const transform = event.source.getFreeDragPosition();
    node.position = { x: node.position.x + transform.x, y: node.position.y + transform.y };
    event.source.reset();
    this.nodes.set([...this.nodes()]);
  }

  startDrawing(event: MouseEvent, node: FlowNode, portId: string, portType: string) {
    if (portType !== 'out') return;
    event.stopPropagation();
    const startX = node.position.x + 160;
    const startY = node.position.y + 54;
    this.drawing.set({ isDrawing: true, sourceNodeId: node.id, sourcePortId: portId, startX, startY, endX: startX, endY: startY });
  }

  finishDrawing(event: MouseEvent, targetNode: FlowNode, targetPortId: string, portType: string) {
    if (portType !== 'in') return;
    event.stopPropagation();
    const d = this.drawing();
    if (d && d.isDrawing && d.sourceNodeId !== targetNode.id) {
      const newConnection: FlowConnection = {
        id: `conn-${Date.now()}`,
        sourceNodeId: d.sourceNodeId,
        sourcePortId: d.sourcePortId,
        targetNodeId: targetNode.id,
        targetPortId: targetPortId
      };
      if (!this.hasInfiniteLoop(newConnection)) {
        this.connections.update(conns => [...conns, newConnection]);
      } else {
        alert('Ação bloqueada: Loop infinito detectado.');
      }
    }
    this.drawing.set(null);
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent) {
    const d = this.drawing();
    if (d && d.isDrawing) {
      const rect = this.canvas.nativeElement.getBoundingClientRect();
      this.drawing.set({ ...d, endX: event.clientX - rect.left, endY: event.clientY - rect.top });
    }
  }

  @HostListener('document:mouseup')
  onMouseUp() {
    this.drawing.set(null);
  }

  getConnectionPath(conn: FlowConnection): string {
    const source = this.nodes().find(n => n.id === conn.sourceNodeId);
    const target = this.nodes().find(n => n.id === conn.targetNodeId);
    if (!source || !target) return '';
    const startX = source.position.x + 160;
    const startY = source.position.y + 54;
    const endX = target.position.x;
    const endY = target.position.y + 54;
    return `M ${startX} ${startY} C ${startX + 50} ${startY}, ${endX - 50} ${endY}, ${endX} ${endY}`;
  }

  getDrawingPath(): string {
    const d = this.drawing();
    if (!d) return '';
    return `M ${d.startX} ${d.startY} C ${d.startX + 50} ${d.startY}, ${d.endX - 50} ${d.endY}, ${d.endX} ${d.endY}`;
  }

  hasInfiniteLoop(newConnection: FlowConnection): boolean {
    const adjList = new Map<string, string[]>();
    const allConnections = [...this.connections(), newConnection];
    for (const conn of allConnections) {
      if (!adjList.has(conn.sourceNodeId)) adjList.set(conn.sourceNodeId, []);
      adjList.get(conn.sourceNodeId)!.push(conn.targetNodeId);
    }
    const visited = new Set<string>();
    const recStack = new Set<string>();
    const isCyclic = (nodeId: string): boolean => {
      if (!visited.has(nodeId)) {
        visited.add(nodeId);
        recStack.add(nodeId);
        const neighbors = adjList.get(nodeId) || [];
        for (const neighbor of neighbors) {
          if (!visited.has(neighbor) && isCyclic(neighbor)) return true;
          else if (recStack.has(neighbor)) return true;
        }
      }
      recStack.delete(nodeId);
      return false;
    };
    for (const node of this.nodes()) {
      if (isCyclic(node.id)) return true;
    }
    return false;
  }
}
