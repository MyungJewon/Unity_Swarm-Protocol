# Project: Legion Core (Interactive Aquarium)
### Mobile Massive Unit Battle Simulator & Aquarium
> **Extreme Mobile Optimization Showcase**: Rendering 10,000+ dynamic units at 60FPS on Mobile Devices with Interactive Elements.

![Badge](https://img.shields.io/badge/Unity-6000.3.0f1-black?logo=unity)
![Badge](https://img.shields.io/badge/Platform-Android%20%2F%20PC-green)
![Badge](https://img.shields.io/badge/Tech-DOTS%20%26%20GPU%20Instancing-blue)

---

## Project Overview (개요)
**Legion Core**는 모바일 디바이스의 하드웨어 한계를 시험하고 극복하기 위해 개발된 **대규모 실시간 군집 시뮬레이션 프로젝트**입니다.
기존의 데이터 지향 기술 스택(DOTS) 기반 렌더링에 **사용자 상호작용(Interaction)**을 더하여, 10,000마리의 물고기가 유저의 입력에 반응하고 먹이를 쫓는 **3D 수족관**을 구현했습니다.

작업기간 : 2026.01.24 ~ 2026.01.29

### Key Objectives & Features
* **Massive Rendering:** GPU Instancing(`DrawMeshInstancedIndirect`)을 활용하여 10,000+ 객체를 단일 드로우 콜로 처리.
* **High Performance Simulation:** Unity Job System과 Burst Compiler로 이동, 회전, 회피 연산을 병렬 처리.
* **Interactive Environment:**
    * **Feeding System:** 화면 터치/클릭으로 먹이를 투하하면 군집이 목표 지점으로 모여드는(Seek) 로직 구현.
    * **Responsive Control:** PC(마우스)와 모바일(터치)을 모두 지원하는 RTS 스타일의 Orbit Camera.
* **Spatial Partitioning:** 커스텀 그리드 해싱을 통한 효율적인 주변 이웃 탐색 및 충돌 회피.

---

## Technical Details (기술 명세)

### 1. Core Simulation (DOTS & Jobs)
`LegionManager`가 메인 스레드에서 시뮬레이션을 관리하며, 실제 무거운 연산은 작업 스레드로 분산됩니다.
* **Boundary Logic:** 수조 크기(`Tank Size`)를 정의하고, 유닛이 경계를 벗어나면 부드럽게 내부로 회전시키는 로직을 Job 내부에서 병렬 처리하여 연산 비용 최소화.
* **Wander & Seek:** 평소에는 노이즈 기반으로 자연스럽게 유영(Wander)하다가, 먹이가 감지되면 해당 위치로 부드럽게 방향을 전환(Seek)하는 상태 머신 구현.

### 2. Interaction & Camera System
사용자 경험(UX)을 고려한 부드러운 조작감을 구현했습니다.
* **Input Unification:** 마우스(PC)와 터치(Mobile) 입력을 하나의 로직으로 통합 처리.
* **Smooth Damping:** 카메라의 회전, 줌, 이동에 `Mathf.Lerp`를 적용하여 급격한 화면 전환 방지 및 부드러운 감속 효과(Inertia) 구현.
* **Raycast Interaction:** 화면 터치 시 `ScreenPointToRay`를 통해 수면(Water Layer)과의 교차점을 계산하여 정확한 위치에 먹이 생성.

### 3. Rendering Optimization
* **GPU Instancing:** 각 유닛의 위치, 회전, 애니메이션 오프셋 데이터를 `ComputeBuffer`에 담아 쉐이더로 직접 전달.
* **Procedural Animation:** 버텍스 쉐이더(Vertex Shader)에서 시간과 오프셋을 기반으로 물고기의 유영 애니메이션을 처리하여 CPU/메모리 오버헤드 제거.

---

## Controls (조작 방법)

이 프로젝트는 PC와 모바일 환경을 모두 지원합니다.

| Action | PC (Mouse) | Mobile (Touch) |
| :--- | :--- | :--- |
| **Rotate Camera** | 우클릭 드래그 | 한 손가락 드래그 |
| **Zoom In/Out** | 마우스 휠 | 두 손가락 핀치(Pinch) |
| **Feed Fish** | 좌클릭 (수면 클릭) | 화면 탭 (Tap) |

---
## Performance Metrics (성능 지표)
*Test Device: Galaxy Z Fold7*
[![Legion Core](https://img.youtube.com/vi/J--UmX4KyIo/hqdefault.jpg)](https://youtube.com/shorts/J--UmX4KyIo)
